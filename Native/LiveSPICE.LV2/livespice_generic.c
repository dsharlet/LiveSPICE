#include <dlfcn.h>
#include <pthread.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include <coreclr_delegates.h>
// hostfxr.h uses this struct in a parameter list before declaring it, which C rejects under -Werror.
struct hostfxr_dotnet_environment_info;
#include <hostfxr.h>
#include <nethost.h>

#include <lv2/atom/forge.h>
#include <lv2/atom/util.h>
#include <lv2/core/lv2.h>
#include <lv2/patch/patch.h>
#include <lv2/state/state.h>
#include <lv2/urid/urid.h>
#include <lv2/worker/worker.h>

#define LIVESPICE_GENERIC_URI "https://livespice.org/plugins/generic"
#define LIVESPICE__schematicPath "https://livespice.org/ns/plugin#schematicPath"
#define LIVESPICE__control "https://livespice.org/ns/plugin#control"
#define LIVESPICE__controlName "https://livespice.org/ns/plugin#controlName"
#define LIVESPICE__controlValue "https://livespice.org/ns/plugin#controlValue"
#define LIVESPICE__controls "https://livespice.org/ns/plugin#controls"
#define MAX_BLOCK 4096
#define MAX_CONTROLS 32

static struct {
    int (CORECLR_DELEGATE_CALLTYPE* create)(const char* path, int sample_rate, int max_block, void** handle);
    int (CORECLR_DELEGATE_CALLTYPE* set_control)(void* handle, const char* name, double value);
    int (CORECLR_DELEGATE_CALLTYPE* process)(void* handle, const float* input, float* output, int count);
    int (CORECLR_DELEGATE_CALLTYPE* destroy)(void* handle);
} bridge;
static pthread_once_t bridge_once = PTHREAD_ONCE_INIT;
static __thread const char* bridge_bundle_path;

// The host dlopens plugins RTLD_LOCAL, so hostfxr exports are resolved through our own handle; the runtime is never shut down
// because CoreCLR cannot be reloaded in the same process.
static void load_bridge(void)
{
    char_t hostfxr_path[4096];
    size_t size = sizeof(hostfxr_path) / sizeof(char_t);
    void* hostfxr = get_hostfxr_path(hostfxr_path, &size, NULL) == 0 ? dlopen(hostfxr_path, RTLD_NOW | RTLD_LOCAL) : NULL;
    if (hostfxr == NULL) {
        fprintf(stderr, "LiveSPICE: .NET hostfxr not found, install the .NET 8 runtime or set DOTNET_ROOT\n");
        return;
    }

    hostfxr_initialize_for_runtime_config_fn initialize = (hostfxr_initialize_for_runtime_config_fn)dlsym(hostfxr, "hostfxr_initialize_for_runtime_config");
    hostfxr_get_runtime_delegate_fn get_delegate = (hostfxr_get_runtime_delegate_fn)dlsym(hostfxr, "hostfxr_get_runtime_delegate");
    hostfxr_close_fn close = (hostfxr_close_fn)dlsym(hostfxr, "hostfxr_close");

    char config[4096], assembly[4096];
    snprintf(config, sizeof(config), "%smanaged/LiveSPICE.NativeBridge.runtimeconfig.json", bridge_bundle_path);
    snprintf(assembly, sizeof(assembly), "%smanaged/LiveSPICE.NativeBridge.dll", bridge_bundle_path);
    hostfxr_handle context = NULL;
    int status = initialize(config, NULL, &context);
    load_assembly_and_get_function_pointer_fn load = NULL;
    if (status >= 0 && status <= 2 && context != NULL)
        status = get_delegate(context, hdt_load_assembly_and_get_function_pointer, (void**)&load);
    if (context != NULL)
        close(context);
    if (load == NULL) {
        fprintf(stderr, "LiveSPICE: cannot start .NET from %s (0x%x)\n", config, (unsigned)status);
        return;
    }

    // Create is fetched last so a partial failure leaves it NULL and the plugin stays passthrough.
    const char* type = "LiveSPICE.NativeBridge.Bridge, LiveSPICE.NativeBridge";
    if (load(assembly, type, "SetControl", UNMANAGEDCALLERSONLY_METHOD, NULL, (void**)&bridge.set_control) == 0
        && load(assembly, type, "Process", UNMANAGEDCALLERSONLY_METHOD, NULL, (void**)&bridge.process) == 0
        && load(assembly, type, "Destroy", UNMANAGEDCALLERSONLY_METHOD, NULL, (void**)&bridge.destroy) == 0
        && load(assembly, type, "Create", UNMANAGEDCALLERSONLY_METHOD, NULL, (void**)&bridge.create) == 0)
        return;
    bridge.create = NULL;
    fprintf(stderr, "LiveSPICE: cannot load the bridge from %s\n", assembly);
}

typedef enum { JOB_LOAD, JOB_CONTROL, JOB_DESTROY } JobKind;

typedef struct {
    JobKind kind;
    void* engine;
    double value;
    char text[4096];
} Job;

typedef enum {
    PORT_INPUT = 0,
    PORT_OUTPUT = 1,
    PORT_CONTROL_EVENTS = 2,
    PORT_NOTIFY = 3
} PortIndex;

typedef struct {
    char name[128];
    double value;
    bool dirty;
} Control;

typedef struct {
    const float* input;
    float* output;
    const LV2_Atom_Sequence* control_events;
    LV2_Atom_Sequence* notify;
    char schematic_path[4096];
    char pending_path[4096];
    bool load_pending;
    bool notify_pending;
    Control controls[MAX_CONTROLS];
    int control_count;
    char* bundle_path;
    double sample_rate;
    void* engine;
    void* retired;
    LV2_URID_Map* map;
    LV2_Worker_Schedule* schedule;
    LV2_Atom_Forge forge;
    LV2_URID schematic_path_key;
    LV2_URID controls_key;
    LV2_URID control;
    LV2_URID control_name;
    LV2_URID control_value;
    LV2_URID patch_get;
} LiveSpiceGeneric;

static void map_features(LiveSpiceGeneric* self, const LV2_Feature* const* features)
{
    for (const LV2_Feature* const* feature = features; feature != NULL && *feature != NULL; feature++) {
        if (strcmp((*feature)->URI, LV2_URID__map) == 0)
            self->map = (LV2_URID_Map*)(*feature)->data;
        else if (strcmp((*feature)->URI, LV2_WORKER__schedule) == 0)
            self->schedule = (LV2_Worker_Schedule*)(*feature)->data;
    }

    if (self->map != NULL) {
        lv2_atom_forge_init(&self->forge, self->map);
        self->schematic_path_key = self->map->map(self->map->handle, LIVESPICE__schematicPath);
        self->controls_key = self->map->map(self->map->handle, LIVESPICE__controls);
        self->control = self->map->map(self->map->handle, LIVESPICE__control);
        self->control_name = self->map->map(self->map->handle, LIVESPICE__controlName);
        self->control_value = self->map->map(self->map->handle, LIVESPICE__controlValue);
        self->patch_get = self->map->map(self->map->handle, LV2_PATCH__Get);
    }
}

static void set_schematic_path(LiveSpiceGeneric* self, const char* path, uint32_t size)
{
    size_t length = strnlen(path, size < sizeof(self->pending_path) ? size : sizeof(self->pending_path) - 1);
    memcpy(self->pending_path, path, length);
    self->pending_path[length] = '\0';
    self->load_pending = true;
    self->control_count = 0;
}

static void read_control(LiveSpiceGeneric* self, const LV2_Atom_Object* object)
{
    const LV2_Atom* name = NULL;
    const LV2_Atom* value = NULL;
    lv2_atom_object_get(object, self->control_name, &name, self->control_value, &value, 0);
    if (name == NULL || name->type != self->forge.String || value == NULL || value->type != self->forge.Double)
        return;

    int i = 0;
    while (i < self->control_count && strncmp(self->controls[i].name, LV2_ATOM_BODY_CONST(name), sizeof(self->controls[i].name) - 1) != 0)
        i++;
    if (i == MAX_CONTROLS)
        return;
    if (i == self->control_count)
        snprintf(self->controls[self->control_count++].name, sizeof(self->controls[i].name), "%s", (const char*)LV2_ATOM_BODY_CONST(name));
    self->controls[i].value = ((const LV2_Atom_Double*)value)->body;
    self->controls[i].dirty = true;
}

static void write_control(LiveSpiceGeneric* self, LV2_Atom_Forge* forge, const Control* control)
{
    LV2_Atom_Forge_Frame frame;
    lv2_atom_forge_object(forge, &frame, 0, self->control);
    lv2_atom_forge_key(forge, self->control_name);
    lv2_atom_forge_string(forge, control->name, (uint32_t)strlen(control->name));
    lv2_atom_forge_key(forge, self->control_value);
    lv2_atom_forge_double(forge, control->value);
    lv2_atom_forge_pop(forge, &frame);
}

static bool schedule_job(LiveSpiceGeneric* self, JobKind kind, void* engine, double value, const char* text)
{
    Job job;
    job.kind = kind;
    job.engine = engine;
    job.value = value;
    size_t length = strnlen(text, sizeof(job.text) - 1);
    memcpy(job.text, text, length);
    job.text[length] = '\0';
    return self->schedule->schedule_work(self->schedule->handle, (uint32_t)(offsetof(Job, text) + length + 1), &job) == LV2_WORKER_SUCCESS;
}

static void read_control_events(LiveSpiceGeneric* self)
{
    if (self->control_events == NULL || self->control_events->atom.type == 0)
        return;

    LV2_ATOM_SEQUENCE_FOREACH(self->control_events, event) {
        const LV2_Atom_Object* object = (const LV2_Atom_Object*)&event->body;
        if (event->body.type == self->forge.Object && object->body.otype == self->control)
            read_control(self, object);
        if (event->body.type == self->forge.Object && object->body.otype == self->patch_get)
            self->notify_pending = true;
        if (event->body.type != self->forge.Path && event->body.type != self->forge.String)
            continue;

        const char* path = (const char*)LV2_ATOM_BODY(&event->body);
        set_schematic_path(self, path, event->body.size);
    }
}

static LV2_Handle instantiate(const LV2_Descriptor* descriptor, double sample_rate, const char* bundle_path, const LV2_Feature* const* features)
{
    (void)descriptor;

    LiveSpiceGeneric* self = (LiveSpiceGeneric*)calloc(1, sizeof(LiveSpiceGeneric));
    if (self == NULL)
        return NULL;

    map_features(self, features);
    if (self->map == NULL || self->schedule == NULL) {
        free(self);
        return NULL;
    }
    self->bundle_path = strdup(bundle_path);
    self->sample_rate = sample_rate;
    return (LV2_Handle)self;
}

static void connect_port(LV2_Handle instance, uint32_t port, void* data)
{
    LiveSpiceGeneric* self = (LiveSpiceGeneric*)instance;
    switch ((PortIndex)port) {
    case PORT_INPUT:
        self->input = (const float*)data;
        break;
    case PORT_OUTPUT:
        self->output = (float*)data;
        break;
    case PORT_CONTROL_EVENTS:
        self->control_events = (const LV2_Atom_Sequence*)data;
        break;
    case PORT_NOTIFY:
        self->notify = (LV2_Atom_Sequence*)data;
        break;
    }
}

static void activate(LV2_Handle instance)
{
    (void)instance;
}

static void write_notify(LiveSpiceGeneric* self)
{
    lv2_atom_forge_set_buffer(&self->forge, (uint8_t*)self->notify, self->notify->atom.size);
    LV2_Atom_Forge_Frame frame;
    lv2_atom_forge_sequence_head(&self->forge, &frame, 0);
    if (self->notify_pending) {
        lv2_atom_forge_frame_time(&self->forge, 0);
        lv2_atom_forge_path(&self->forge, self->pending_path, (uint32_t)strlen(self->pending_path));
        for (int i = 0; i < self->control_count; i++) {
            lv2_atom_forge_frame_time(&self->forge, 0);
            write_control(self, &self->forge, &self->controls[i]);
        }
        self->notify_pending = false;
    }
    lv2_atom_forge_pop(&self->forge, &frame);
}

static void run(LV2_Handle instance, uint32_t sample_count)
{
    LiveSpiceGeneric* self = (LiveSpiceGeneric*)instance;
    read_control_events(self);
    write_notify(self);
    if (self->load_pending && schedule_job(self, JOB_LOAD, NULL, 0, self->pending_path))
        self->load_pending = false;

    for (int i = 0; self->engine != NULL && i < self->control_count; i++)
        if (self->controls[i].dirty && schedule_job(self, JOB_CONTROL, self->engine, self->controls[i].value, self->controls[i].name))
            self->controls[i].dirty = false;

    if (self->retired != NULL && schedule_job(self, JOB_DESTROY, self->retired, 0, ""))
        self->retired = NULL;

    if (self->input == NULL || self->output == NULL)
        return;

    if (self->engine != NULL && bridge.process(self->engine, self->input, self->output, (int)sample_count) != 0) {
        fprintf(stderr, "LiveSPICE: simulation failed, passing audio through until the schematic is reloaded\n");
        self->retired = self->engine;
        self->engine = NULL;
    }
    if (self->engine == NULL)
        memmove(self->output, self->input, sample_count * sizeof(float));
}

static LV2_Worker_Status work(LV2_Handle instance, LV2_Worker_Respond_Function respond, LV2_Worker_Respond_Handle handle, uint32_t size, const void* data)
{
    LiveSpiceGeneric* self = (LiveSpiceGeneric*)instance;
    const Job* job = (const Job*)data;
    if (job->kind == JOB_DESTROY)
        bridge.destroy(job->engine);
    if (job->kind == JOB_CONTROL)
        bridge.set_control(job->engine, job->text, job->value);
    if (job->kind != JOB_LOAD)
        return LV2_WORKER_SUCCESS;

    Job loaded;
    memcpy(&loaded, job, size);
    if (job->text[0] != '\0') {
        bridge_bundle_path = self->bundle_path;
        pthread_once(&bridge_once, load_bridge);
        if (bridge.create == NULL || bridge.create(job->text, (int)self->sample_rate, MAX_BLOCK, &loaded.engine) != 0)
            return LV2_WORKER_ERR_UNKNOWN;
    }
    return respond(handle, size, &loaded);
}

static LV2_Worker_Status work_response(LV2_Handle instance, uint32_t size, const void* body)
{
    LiveSpiceGeneric* self = (LiveSpiceGeneric*)instance;
    const Job* loaded = (const Job*)body;
    self->retired = self->engine;
    self->engine = loaded->engine;
    memcpy(self->schematic_path, loaded->text, size - offsetof(Job, text));
    for (int i = 0; i < self->control_count; i++)
        self->controls[i].dirty = true;
    return LV2_WORKER_SUCCESS;
}

static void deactivate(LV2_Handle instance)
{
    (void)instance;
}

static void cleanup(LV2_Handle instance)
{
    LiveSpiceGeneric* self = (LiveSpiceGeneric*)instance;
    if (self->engine != NULL)
        bridge.destroy(self->engine);
    if (self->retired != NULL)
        bridge.destroy(self->retired);
    free(self->bundle_path);
    free(instance);
}

static LV2_State_Status save_state(LV2_Handle instance, LV2_State_Store_Function store, LV2_State_Handle handle, uint32_t flags, const LV2_Feature* const* features)
{
    (void)flags;
    (void)features;

    LiveSpiceGeneric* self = (LiveSpiceGeneric*)instance;
    if (self->schematic_path[0] == '\0')
        return LV2_STATE_SUCCESS;

    LV2_State_Status status = store(handle, self->schematic_path_key, self->schematic_path, strlen(self->schematic_path) + 1, self->forge.Path, LV2_STATE_IS_POD);
    uint8_t buffer[MAX_CONTROLS * 256];
    LV2_Atom_Forge forge = self->forge;
    lv2_atom_forge_set_buffer(&forge, buffer, sizeof(buffer));
    LV2_Atom_Forge_Frame frame;
    lv2_atom_forge_tuple(&forge, &frame);
    for (int i = 0; i < self->control_count; i++)
        write_control(self, &forge, &self->controls[i]);
    lv2_atom_forge_pop(&forge, &frame);
    const LV2_Atom* tuple = (const LV2_Atom*)buffer;
    if (status == LV2_STATE_SUCCESS && self->control_count > 0)
        status = store(handle, self->controls_key, LV2_ATOM_BODY_CONST(tuple), tuple->size, self->forge.Tuple, LV2_STATE_IS_POD);
    return status;
}

static LV2_State_Status restore_state(LV2_Handle instance, LV2_State_Retrieve_Function retrieve, LV2_State_Handle handle, uint32_t flags, const LV2_Feature* const* features)
{
    (void)flags;
    (void)features;

    LiveSpiceGeneric* self = (LiveSpiceGeneric*)instance;
    size_t size = 0;
    uint32_t type = 0;
    uint32_t value_flags = 0;
    const void* value = retrieve(handle, self->schematic_path_key, &size, &type, &value_flags);
    if (value == NULL || size == 0)
        return LV2_STATE_SUCCESS;
    if (type != self->forge.Path)
        return LV2_STATE_ERR_BAD_TYPE;

    set_schematic_path(self, (const char*)value, (uint32_t)size);
    value = retrieve(handle, self->controls_key, &size, &type, &value_flags);
    if (value != NULL && type == self->forge.Tuple)
        LV2_ATOM_TUPLE_BODY_FOREACH(value, size, item)
            if (item->type == self->forge.Object && ((const LV2_Atom_Object*)item)->body.otype == self->control)
                read_control(self, (const LV2_Atom_Object*)item);
    self->notify_pending = true;
    return LV2_STATE_SUCCESS;
}

static const LV2_State_Interface state_interface = {
    save_state,
    restore_state
};

static const LV2_Worker_Interface worker_interface = {
    work,
    work_response,
    NULL
};

static const void* extension_data(const char* uri)
{
    if (strcmp(uri, LV2_STATE__interface) == 0)
        return &state_interface;
    if (strcmp(uri, LV2_WORKER__interface) == 0)
        return &worker_interface;
    return NULL;
}

static const LV2_Descriptor descriptor = {
    LIVESPICE_GENERIC_URI,
    instantiate,
    connect_port,
    activate,
    run,
    deactivate,
    cleanup,
    extension_data
};

LV2_SYMBOL_EXPORT const LV2_Descriptor* lv2_descriptor(uint32_t index)
{
    return index == 0 ? &descriptor : NULL;
}
