#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#include <lv2/atom/atom.h>
#include <lv2/atom/util.h>
#include <lv2/core/lv2.h>
#include <lv2/state/state.h>
#include <lv2/urid/urid.h>

#define LIVESPICE_GENERIC_URI "https://livespice.org/plugins/generic"
#define LIVESPICE__schematicPath "https://livespice.org/ns/plugin#schematicPath"

typedef enum {
    PORT_INPUT = 0,
    PORT_OUTPUT = 1,
    PORT_CONTROL_EVENTS = 2
} PortIndex;

typedef struct {
    const float* input;
    float* output;
    const LV2_Atom_Sequence* control_events;
    char* schematic_path;
    LV2_URID_Map* map;
    LV2_URID atom_path;
    LV2_URID atom_string;
    LV2_URID schematic_path_key;
} LiveSpiceGeneric;

static void map_features(LiveSpiceGeneric* self, const LV2_Feature* const* features)
{
    for (const LV2_Feature* const* feature = features; feature != NULL && *feature != NULL; feature++) {
        if (strcmp((*feature)->URI, LV2_URID__map) == 0)
            self->map = (LV2_URID_Map*)(*feature)->data;
    }

    if (self->map != NULL) {
        self->atom_path = self->map->map(self->map->handle, LV2_ATOM__Path);
        self->atom_string = self->map->map(self->map->handle, LV2_ATOM__String);
        self->schematic_path_key = self->map->map(self->map->handle, LIVESPICE__schematicPath);
    }
}

static void set_schematic_path(LiveSpiceGeneric* self, const char* path, uint32_t size)
{
    if (path == NULL)
        return;

    char* copy = (char*)calloc((size_t)size + 1, sizeof(char));
    if (copy == NULL)
        return;

    if (size > 0)
        memcpy(copy, path, size);
    copy[size] = '\0';

    free(self->schematic_path);
    self->schematic_path = copy;
}

static void read_control_events(LiveSpiceGeneric* self)
{
    if (self->control_events == NULL || self->control_events->atom.type == 0)
        return;

    LV2_ATOM_SEQUENCE_FOREACH(self->control_events, event) {
        if (event->body.type != self->atom_path && event->body.type != self->atom_string)
            continue;

        const char* path = (const char*)LV2_ATOM_BODY(&event->body);
        set_schematic_path(self, path, event->body.size);
    }
}

static LV2_Handle instantiate(const LV2_Descriptor* descriptor, double sample_rate, const char* bundle_path, const LV2_Feature* const* features)
{
    (void)descriptor;
    (void)sample_rate;
    (void)bundle_path;

    LiveSpiceGeneric* self = (LiveSpiceGeneric*)calloc(1, sizeof(LiveSpiceGeneric));
    if (self != NULL)
        map_features(self, features);
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
    }
}

static void activate(LV2_Handle instance)
{
    (void)instance;
}

static void run(LV2_Handle instance, uint32_t sample_count)
{
    LiveSpiceGeneric* self = (LiveSpiceGeneric*)instance;
    read_control_events(self);

    if (self->input == NULL || self->output == NULL)
        return;

    memcpy(self->output, self->input, sample_count * sizeof(float));
}

static void deactivate(LV2_Handle instance)
{
    (void)instance;
}

static void cleanup(LV2_Handle instance)
{
    LiveSpiceGeneric* self = (LiveSpiceGeneric*)instance;
    free(self->schematic_path);
    free(instance);
}

static LV2_State_Status save_state(LV2_Handle instance, LV2_State_Store_Function store, LV2_State_Handle handle, uint32_t flags, const LV2_Feature* const* features)
{
    (void)flags;
    (void)features;

    LiveSpiceGeneric* self = (LiveSpiceGeneric*)instance;
    if (self->schematic_path_key == 0 || self->atom_path == 0 || self->schematic_path == NULL || self->schematic_path[0] == '\0')
        return LV2_STATE_SUCCESS;

    return store(
        handle,
        self->schematic_path_key,
        self->schematic_path,
        strlen(self->schematic_path) + 1,
        self->atom_path,
        LV2_STATE_IS_POD);
}

static LV2_State_Status restore_state(LV2_Handle instance, LV2_State_Retrieve_Function retrieve, LV2_State_Handle handle, uint32_t flags, const LV2_Feature* const* features)
{
    (void)flags;
    (void)features;

    LiveSpiceGeneric* self = (LiveSpiceGeneric*)instance;
    if (self->schematic_path_key == 0)
        return LV2_STATE_ERR_NO_FEATURE;

    size_t size = 0;
    uint32_t type = 0;
    uint32_t value_flags = 0;
    const void* value = retrieve(handle, self->schematic_path_key, &size, &type, &value_flags);
    if (value == NULL || size == 0)
        return LV2_STATE_SUCCESS;
    if (type != self->atom_path)
        return LV2_STATE_ERR_BAD_TYPE;

    set_schematic_path(self, (const char*)value, (uint32_t)size);
    return LV2_STATE_SUCCESS;
}

static const LV2_State_Interface state_interface = {
    save_state,
    restore_state
};

static const void* extension_data(const char* uri)
{
    if (strcmp(uri, LV2_STATE__interface) == 0)
        return &state_interface;
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
