CC ?= gcc
CFLAGS ?= -O3 -fPIC -Wall -Wextra -Werror
LDFLAGS ?= -shared
GTK3_CFLAGS := $(shell pkg-config --cflags gtk+-3.0)
GTK3_LIBS := $(shell pkg-config --libs gtk+-3.0)
BUNDLE := build/LiveSPICE.lv2
MXR_PLUGIN := $(BUNDLE)/livespice_mxr_phase90.so
GENERIC_PLUGIN := $(BUNDLE)/livespice_generic.so
GENERIC_UI := $(BUNDLE)/livespice_generic_ui.so
UI_SMOKE := build/livespice_generic_ui_smoke
TEST_HOST := build/test_host
DOTNET_ROOT ?= /usr/lib/dotnet
NETHOST_DIR := $(lastword $(wildcard $(DOTNET_ROOT)/packs/Microsoft.NETCore.App.Host.linux-x64/*/runtimes/linux-x64/native $(DOTNET_ROOT)/packs/Microsoft.NETCore.App.Host.ubuntu*-x64/*/runtimes/ubuntu*-x64/native))
TTL := $(BUNDLE)/manifest.ttl $(BUNDLE)/livespice_mxr_phase90.ttl $(BUNDLE)/livespice_generic.ttl
ASSETS := $(BUNDLE)/MetalAlpha.png
PREFIX ?= $(HOME)/.lv2
UI_SMOKE_SCHEMATIC ?= ../../Tests/Examples/MXR\ Phase\ 90.schx
UI_SMOKE_IMAGE ?= ../../out/lv2-generic-ui-smoke.png

.PHONY: all managed install clean test test-sim ui-smoke

all: $(MXR_PLUGIN) $(GENERIC_PLUGIN) $(GENERIC_UI) $(TTL) $(ASSETS)

$(BUNDLE):
	mkdir -p $(BUNDLE)

managed: | $(BUNDLE)
	dotnet publish ../../LiveSPICE.NativeBridge -c Release -o $(BUNDLE)/managed

$(MXR_PLUGIN): livespice_mxr_phase90.c | $(BUNDLE)
	$(CC) $(CFLAGS) $< $(LDFLAGS) -lm -o $@

$(GENERIC_PLUGIN): livespice_generic.c | $(BUNDLE)
	$(if $(NETHOST_DIR),,$(error libnethost not found under $(DOTNET_ROOT)/packs, install the .NET 8 SDK or set DOTNET_ROOT))
	$(CC) $(CFLAGS) -I$(NETHOST_DIR) $< $(LDFLAGS) $(NETHOST_DIR)/libnethost.a -lstdc++ -ldl -lpthread -lm -o $@

$(GENERIC_UI): livespice_generic_ui.c | $(BUNDLE)
	$(CC) $(CFLAGS) $(GTK3_CFLAGS) $< $(LDFLAGS) $(GTK3_LIBS) -o $@

$(UI_SMOKE): livespice_generic_ui.c $(ASSETS) | $(BUNDLE)
	$(CC) $(CFLAGS) $(GTK3_CFLAGS) -DLIVESPICE_UI_SMOKE $< $(GTK3_LIBS) -lm -o $@

$(BUNDLE)/manifest.ttl: manifest.ttl | $(BUNDLE)
	cp $< $@

$(BUNDLE)/livespice_mxr_phase90.ttl: livespice_mxr_phase90.ttl | $(BUNDLE)
	cp $< $@

$(BUNDLE)/livespice_generic.ttl: livespice_generic.ttl | $(BUNDLE)
	cp $< $@

$(BUNDLE)/MetalAlpha.png: ../../LiveSPICEVst/Images/MetalAlpha.png | $(BUNDLE)
	cp $< $@

install: all managed
	mkdir -p $(PREFIX)
	rm -rf $(PREFIX)/LiveSPICE.lv2
	cp -r $(BUNDLE) $(PREFIX)/

test: all
	LV2_PATH=$(CURDIR)/build lv2ls | grep -F https://livespice.org/plugins/mxr-phase90
	LV2_PATH=$(CURDIR)/build lv2ls | grep -F https://livespice.org/plugins/generic

$(TEST_HOST): test_host.c | $(BUNDLE)
	$(CC) $(CFLAGS) $< -ldl -lm -o $@

test-sim: all managed $(TEST_HOST)
	$(TEST_HOST) $(CURDIR)/$(BUNDLE)/ "$(CURDIR)/../../Tests/Examples/MXR Distortion +.schx" Drive

ui-smoke: $(UI_SMOKE)
	mkdir -p ../../out
	$(UI_SMOKE) $(CURDIR)/$(BUNDLE) $(UI_SMOKE_SCHEMATIC) $(UI_SMOKE_IMAGE)

clean:
	rm -rf build
