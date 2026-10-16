#include <dlfcn.h>
#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

#include <lv2/atom/forge.h>
#include <lv2/core/lv2.h>
#include <lv2/state/state.h>
#include <lv2/urid/urid.h>
#include <lv2/worker/worker.h>

#define RATE 48000
#define BLOCK 256

static const char* uris[64];
static const char* schematic;
typedef struct {
    unsigned char data[8][4096];
    uint32_t size[8], count;
} Queue;

static Queue jobs, responses;

static LV2_URID map_uri(LV2_URID_Map_Handle handle, const char* uri)
{
    (void)handle;
    LV2_URID id = 0;
    while (uris[id] != NULL && strcmp(uris[id], uri) != 0)
        id++;
    uris[id] = uris[id] != NULL ? uris[id] : strdup(uri);
    return id + 1;
}

static LV2_URID_Map map = { NULL, map_uri };

static LV2_Worker_Status enqueue(void* handle, uint32_t size, const void* data)
{
    Queue* queue = (Queue*)handle;
    // Same limit as jalv's 4096-byte worker ring, which also stores a 32-bit size per message.
    if (queue->count == 8 || size + sizeof(uint32_t) >= sizeof(queue->data[0]))
        return LV2_WORKER_ERR_NO_SPACE;
    memcpy(queue->data[queue->count], data, size);
    queue->size[queue->count++] = size;
    return LV2_WORKER_SUCCESS;
}

static const void* retrieve(LV2_State_Handle handle, uint32_t key, size_t* size, uint32_t* type, uint32_t* flags)
{
    (void)handle;
    (void)flags;
    if (key != map_uri(NULL, "https://livespice.org/ns/plugin#schematicPath"))
        return NULL;
    *size = strlen(schematic) + 1;
    *type = map_uri(NULL, LV2_ATOM__Path);
    return schematic;
}

typedef struct {
    const LV2_Descriptor* descriptor;
    LV2_Handle handle;
    const LV2_Worker_Interface* worker;
    float input[BLOCK], output[BLOCK];
    union { LV2_Atom_Sequence sequence; unsigned char bytes[1024]; } events;
    long frame;
} Plugin;

static double now(void)
{
    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    return ts.tv_sec + ts.tv_nsec * 1e-9;
}

// Runs one block, then plays the host's worker thread synchronously; returns whether the output differed from the input.
static int run_block(Plugin* p)
{
    for (int i = 0; i < BLOCK; i++, p->frame++)
        p->input[i] = 0.25f * (float)sin(2.0 * M_PI * 440.0 * p->frame / RATE);
    p->descriptor->run(p->handle, BLOCK);
    p->events.sequence.atom.size = sizeof(LV2_Atom_Sequence_Body);
    for (uint32_t i = 0; i < jobs.count; i++)
        p->worker->work(p->handle, enqueue, &responses, jobs.size[i], jobs.data[i]);
    for (uint32_t i = 0; i < responses.count; i++)
        p->worker->work_response(p->handle, responses.size[i], responses.data[i]);
    jobs.count = responses.count = 0;
    return memcmp(p->input, p->output, sizeof(p->input)) != 0;
}

static void open_plugin(Plugin* p, const char* bundle, const LV2_Feature* const* features)
{
    char path[4096];
    snprintf(path, sizeof(path), "%slivespice_generic.so", bundle);
    void* library = dlopen(path, RTLD_NOW | RTLD_LOCAL);
    if (library == NULL || (p->descriptor = ((LV2_Descriptor_Function)dlsym(library, "lv2_descriptor"))(0)) == NULL)
        exit(2);
    p->handle = p->descriptor->instantiate(p->descriptor, RATE, bundle, features);
    p->worker = p->descriptor->extension_data(LV2_WORKER__interface);
    const LV2_State_Interface* state = p->descriptor->extension_data(LV2_STATE__interface);
    if (p->handle == NULL || p->worker == NULL || state->restore(p->handle, retrieve, NULL, 0, NULL) != LV2_STATE_SUCCESS)
        exit(3);
    p->events.sequence.atom.type = map_uri(NULL, LV2_ATOM__Sequence);
    p->events.sequence.atom.size = sizeof(LV2_Atom_Sequence_Body);
    p->descriptor->connect_port(p->handle, 0, p->input);
    p->descriptor->connect_port(p->handle, 1, p->output);
    p->descriptor->connect_port(p->handle, 2, &p->events);
    p->descriptor->activate(p->handle);

    double start = now();
    while (!run_block(p))
        if (now() - start > 60)
            exit(4);
    printf("ready after %.2f s\n", now() - start);
}

static double measure(Plugin* p, double seconds, int print)
{
    double in = 0, out = 0, in_peak = 0, out_peak = 0;
    long n = 0;
    for (; n < seconds * RATE; n += BLOCK) {
        run_block(p);
        for (int i = 0; i < BLOCK; i++) {
            in += p->input[i] * p->input[i];
            out += p->output[i] * p->output[i];
            in_peak = fmax(in_peak, fabs(p->input[i]));
            out_peak = fmax(out_peak, fabs(p->output[i]));
        }
    }
    in = sqrt(in / n);
    out = sqrt(out / n);
    if (print)
        printf("rms in %.4f out %.4f, crest in %.3f out %.3f\n", in, out, in_peak / in, out_peak / out);
    return out;
}

int main(int argc, char** argv)
{
    if (argc < 4)
        return 1;
    schematic = argv[2];
    LV2_Worker_Schedule schedule = { &jobs, enqueue };
    LV2_Feature map_feature = { LV2_URID__map, &map };
    LV2_Feature schedule_feature = { LV2_WORKER__schedule, &schedule };
    const LV2_Feature* features[] = { &map_feature, &schedule_feature, NULL };

    static Plugin first, second;
    open_plugin(&first, argv[1], features);
    measure(&first, 0.5, 0);
    double before = measure(&first, 3.0, 1);

    LV2_Atom_Forge forge;
    lv2_atom_forge_init(&forge, &map);
    lv2_atom_forge_set_buffer(&forge, first.events.bytes, sizeof(first.events.bytes));
    LV2_Atom_Forge_Frame sequence, object;
    lv2_atom_forge_sequence_head(&forge, &sequence, 0);
    lv2_atom_forge_frame_time(&forge, 0);
    lv2_atom_forge_object(&forge, &object, 0, map_uri(NULL, "https://livespice.org/ns/plugin#control"));
    lv2_atom_forge_key(&forge, map_uri(NULL, "https://livespice.org/ns/plugin#controlName"));
    lv2_atom_forge_string(&forge, argv[3], (uint32_t)strlen(argv[3]));
    lv2_atom_forge_key(&forge, map_uri(NULL, "https://livespice.org/ns/plugin#controlValue"));
    lv2_atom_forge_double(&forge, 1.0);
    lv2_atom_forge_pop(&forge, &object);
    lv2_atom_forge_pop(&forge, &sequence);
    printf("set %s = 1.0\n", argv[3]);
    double start = now(), after = before;
    while (fabs(after - before) < 0.05 * before && now() - start < 30)
        after = measure(&first, 0.25, 0);
    printf("control change took %.2f s\n", now() - start);
    measure(&first, 0.5, 0);
    measure(&first, 1.0, 1);

    open_plugin(&second, argv[1], features);
    measure(&second, 1.0, 1);
    first.descriptor->cleanup(first.handle);
    second.descriptor->cleanup(second.handle);
    return fabs(after - before) < 0.05 * before ? 5 : 0;
}
