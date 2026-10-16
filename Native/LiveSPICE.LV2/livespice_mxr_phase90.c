#include <math.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#include <lv2/core/lv2.h>
#include <lv2/atom/atom.h>
#include <lv2/state/state.h>
#include <lv2/urid/urid.h>

#define LIVESPICE_MXR_PHASE90_URI "https://livespice.org/plugins/mxr-phase90"
#define LIVESPICE__schematicPath "https://livespice.org/ns/plugin#schematicPath"

#define MIN_RATE_HZ 0.05f
#define MAX_RATE_HZ 8.0f
#define MIN_SWEEP_HZ 180.0f
#define MAX_SWEEP_HZ 1800.0f
#define STAGE_COUNT 4

typedef enum {
    PORT_SPEED = 0,
    PORT_TRIMMER = 1,
    PORT_INPUT = 2,
    PORT_OUTPUT = 3
} PortIndex;

typedef struct {
    const float* speed;
    const float* trimmer;
    const float* input;
    float* output;
    double sample_rate;
    float phase;
    float x1[STAGE_COUNT];
    float y1[STAGE_COUNT];
    char* schematic_path;
    LV2_URID_Map* map;
    LV2_URID atom_path;
    LV2_URID schematic_path_key;
} LiveSpiceMxrPhase90;

static float clampf(float value, float min, float max)
{
    if (value < min)
        return min;
    if (value > max)
        return max;
    return value;
}

static LV2_Handle instantiate(const LV2_Descriptor* descriptor, double sample_rate, const char* bundle_path, const LV2_Feature* const* features)
{
    (void)descriptor;
    (void)bundle_path;

    LiveSpiceMxrPhase90* self = (LiveSpiceMxrPhase90*)calloc(1, sizeof(LiveSpiceMxrPhase90));
    if (self != NULL) {
        self->sample_rate = sample_rate;
        for (const LV2_Feature* const* feature = features; feature != NULL && *feature != NULL; feature++) {
            if (strcmp((*feature)->URI, LV2_URID__map) == 0)
                self->map = (LV2_URID_Map*)(*feature)->data;
        }
        if (self->map != NULL) {
            self->atom_path = self->map->map(self->map->handle, LV2_ATOM__Path);
            self->schematic_path_key = self->map->map(self->map->handle, LIVESPICE__schematicPath);
        }
    }
    return (LV2_Handle)self;
}

static void connect_port(LV2_Handle instance, uint32_t port, void* data)
{
    LiveSpiceMxrPhase90* self = (LiveSpiceMxrPhase90*)instance;
    switch ((PortIndex)port) {
    case PORT_SPEED:
        self->speed = (const float*)data;
        break;
    case PORT_TRIMMER:
        self->trimmer = (const float*)data;
        break;
    case PORT_INPUT:
        self->input = (const float*)data;
        break;
    case PORT_OUTPUT:
        self->output = (float*)data;
        break;
    }
}

static void activate(LV2_Handle instance)
{
    LiveSpiceMxrPhase90* self = (LiveSpiceMxrPhase90*)instance;
    self->phase = 0.0f;
    memset(self->x1, 0, sizeof(self->x1));
    memset(self->y1, 0, sizeof(self->y1));
}

static float process_allpass(LiveSpiceMxrPhase90* self, float input, float coefficient, uint32_t stage)
{
    float output = coefficient * input + self->x1[stage] - coefficient * self->y1[stage];
    self->x1[stage] = input;
    self->y1[stage] = output;
    return output;
}

static void run(LV2_Handle instance, uint32_t sample_count)
{
    LiveSpiceMxrPhase90* self = (LiveSpiceMxrPhase90*)instance;
    if (self->input == NULL || self->output == NULL)
        return;

    float speed = self->speed != NULL ? clampf(*self->speed, 0.0f, 1.0f) : 0.5f;
    float trimmer = self->trimmer != NULL ? clampf(*self->trimmer, 0.0f, 1.0f) : 0.5f;
    float rate = MIN_RATE_HZ * powf(MAX_RATE_HZ / MIN_RATE_HZ, speed);
    float depth = 0.25f + trimmer * 0.75f;
    float mix = 0.35f + trimmer * 0.45f;
    float phase_increment = rate / (float)self->sample_rate;

    for (uint32_t sample = 0; sample < sample_count; sample++) {
        float lfo = 0.5f + 0.5f * sinf(2.0f * (float)M_PI * self->phase);
        float sweep = MIN_SWEEP_HZ + (MAX_SWEEP_HZ - MIN_SWEEP_HZ) * lfo * depth;
        float tangent = tanf((float)M_PI * sweep / (float)self->sample_rate);
        float coefficient = (tangent - 1.0f) / (tangent + 1.0f);

        float wet = self->input[sample];
        for (uint32_t stage = 0; stage < STAGE_COUNT; stage++)
            wet = process_allpass(self, wet, coefficient, stage);

        self->output[sample] = self->input[sample] * (1.0f - mix) + wet * mix;

        self->phase += phase_increment;
        if (self->phase >= 1.0f)
            self->phase -= floorf(self->phase);
    }
}

static void deactivate(LV2_Handle instance)
{
    (void)instance;
}

static void cleanup(LV2_Handle instance)
{
    LiveSpiceMxrPhase90* self = (LiveSpiceMxrPhase90*)instance;
    free(self->schematic_path);
    free(instance);
}

static LV2_State_Status save_state(LV2_Handle instance, LV2_State_Store_Function store, LV2_State_Handle handle, uint32_t flags, const LV2_Feature* const* features)
{
    (void)flags;
    (void)features;

    LiveSpiceMxrPhase90* self = (LiveSpiceMxrPhase90*)instance;
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

    LiveSpiceMxrPhase90* self = (LiveSpiceMxrPhase90*)instance;
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

    char* restored = (char*)calloc(size + 1, sizeof(char));
    if (restored == NULL)
        return LV2_STATE_ERR_NO_SPACE;
    memcpy(restored, value, size);

    free(self->schematic_path);
    self->schematic_path = restored;
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
    LIVESPICE_MXR_PHASE90_URI,
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
