// AsioWrapper.h

#pragma once

#include <windows.h>
#include <Ole2.h>

#define IEEE754_64FLOAT 1

#include "asio.h"

// The ASIO COM interface.
interface IAsio : public IUnknown
{
	virtual ASIOBool init(void *sysHandle) = 0;
	virtual void getDriverName(char *name) = 0;	
	virtual long getDriverVersion() = 0;
	virtual void getErrorMessage(char *string) = 0;	
	virtual ASIOError start() = 0;
	virtual ASIOError stop() = 0;
	virtual ASIOError getChannels(long *numInputChannels, long *numOutputChannels) = 0;
	virtual ASIOError getLatencies(long *inputLatency, long *outputLatency) = 0;
	virtual ASIOError getBufferSize(long *minSize, long *maxSize, long *preferredSize, long *granularity) = 0;
	virtual ASIOError canSampleRate(ASIOSampleRate sampleRate) = 0;
	virtual ASIOError getSampleRate(ASIOSampleRate *sampleRate) = 0;
	virtual ASIOError setSampleRate(ASIOSampleRate sampleRate) = 0;
	virtual ASIOError getClockSources(ASIOClockSource *clocks, long *numSources) = 0;
	virtual ASIOError setClockSource(long reference) = 0;
	virtual ASIOError getSamplePosition(ASIOSamples *sPos, ASIOTimeStamp *tStamp) = 0;
	virtual ASIOError getChannelInfo(ASIOChannelInfo *info) = 0;
	virtual ASIOError createBuffers(ASIOBufferInfo *bufferInfos, long numChannels, long bufferSize, ASIOCallbacks *callbacks) = 0;
	virtual ASIOError disposeBuffers() = 0;
	virtual ASIOError controlPanel() = 0;
	virtual ASIOError future(long selector,void *opt) = 0;
	virtual ASIOError outputReady() = 0;
};

void BufferSwitchRouter(long Index, ASIOBool Direct);
void SampleRateChangeRouter(double SampleRate);
//long MessageRouter(long selector, long value, void* message, double* opt);
//ASIOTime* BufferSwitchTimeInfoRouter(ASIOTime* params, long doubleBufferIndex, ASIOBool directProcess);

ASIOCallbacks RouterCallbacks = 
{
	BufferSwitchRouter,
	SampleRateChangeRouter,
	NULL,
	NULL
};

namespace AsioWrapper 
{
	using namespace System::Runtime::InteropServices;
	
	public enum SampleType : long 
	{
		Int16MSB	 = ASIOSTInt16MSB,
		Int24MSB	 = ASIOSTInt24MSB,
		Int32MSB	 = ASIOSTInt32MSB,
		Float32MSB	 = ASIOSTFloat32MSB,
		Float64MSB	 = ASIOSTFloat64MSB,
		Int32MSB16	 = ASIOSTInt32MSB16,
		Int32MSB18	 = ASIOSTInt32MSB18,
		Int32MSB20	 = ASIOSTInt32MSB20,
		Int32MSB24	 = ASIOSTInt32MSB24,
		Int16LSB	 = ASIOSTInt16LSB,
		Int24LSB	 = ASIOSTInt24LSB,
		Int32LSB	 = ASIOSTInt32LSB,
		Float32LSB	 = ASIOSTFloat32LSB,
		Float64LSB	 = ASIOSTFloat64LSB,
		Int32LSB16	 = ASIOSTInt32LSB16,
		Int32LSB18	 = ASIOSTInt32LSB18,
		Int32LSB20	 = ASIOSTInt32LSB20,
		Int32LSB24	 = ASIOSTInt32LSB24,
		DSDInt8LSB1	 = ASIOSTDSDInt8LSB1,
		DSDInt8MSB1	 = ASIOSTDSDInt8MSB1,
		DSDInt8NER8	 = ASIOSTDSDInt8NER8,
	};

	public ref class Channel
	{
		System::String^ name;
		SampleType type;
		bool active;
		long group;

	public:
		Channel(ASIOChannelInfo Info)
		{
			name = gcnew System::String(Info.name);
			type = (SampleType)Info.type;
			active = Info.isActive != ASIOFalse;
			group = Info.channelGroup;
		}
		
		property System::String^ Name { System::String^ get() { return name; } }
		property SampleType Type { SampleType get() { return type; } }
		property bool IsActive { bool get() { return active; } }
		property long Group { long get() { return group; } }
	};

	public ref class Buffers
	{
	protected:
		long channel;
		array<void *>^ buffers;

	public:
		Buffers(int Channel) : channel(Channel) { }

		property long Channel { long get() { return channel; } }
		property void * Buffer[int] { void * get(int i) { return buffers[0]; } }

		void SetBuffers(array<void *>^ Buffers) { buffers = Buffers; }
	};

	// Handles 
	public ref class Asio
	{
		IAsio * m_asio;

		static void Check(ASIOError Error)
		{
			if (!(Error == ASE_OK || Error == ASE_SUCCESS))
				throw gcnew System::Exception("ASIO Error");
		}
		static _GUID ToGUID( System::Guid& guid ) 
		{
			array<byte>^ guidData = guid.ToByteArray();
			pin_ptr<byte> data = &(guidData[0]);
			return *(_GUID *)data;
		}

	public:
		Asio(System::Guid guid) : m_asio(NULL)
		{
			HRESULT hr;
			
			GUID clsid = ToGUID(guid);
			LPVOID asio = NULL;
			hr = CoCreateInstance(clsid, NULL, CLSCTX_INPROC_SERVER, clsid, &asio);
			if (FAILED(hr))
				throw gcnew System::Runtime::InteropServices::COMException("Failed to instantiate ASIO interface", hr);
			m_asio = (IAsio*)asio;

			if (!m_asio->init(0))
			{
				m_asio->Release();
				m_asio = NULL;
				throw gcnew System::Runtime::InteropServices::COMException("Failed to initialize ASIO interface", hr);
			}
		}
		~Asio() { this->!Asio(); }
		!Asio() { if (m_asio != NULL) m_asio->Release(); }
		
		property System::String^ DriverName 
		{ 
			System::String^ get() 
			{ 
				char name[256];
				m_asio->getDriverName(name);
				return gcnew System::String(name);
			} 
		}

		property System::String^ ErrorMessage 
		{ 
			System::String^ get() 
			{ 
				char name[256];
				m_asio->getErrorMessage(name);
				return gcnew System::String(name);
			} 
		}

		property long DriverVersion { long get() { return m_asio->getDriverVersion(); } }
		
		void Start() { Check(m_asio->start()); }
		void Stop() { Check(m_asio->stop()); }
		
	private:
		array<Channel^>^ GetChannels(int Count, bool Input)
		{
			array<Channel^>^ channels = gcnew array<Channel^>(Count);
			for (long i = 0; i < Count; ++i)
			{
				ASIOChannelInfo info = { i, Input ? ASIOTrue : ASIOFalse };
				Check(m_asio->getChannelInfo(&info));
				channels[i] = gcnew Channel(info);
			}
			return channels;
		}

	public:
		delegate void BufferSwitchCallback(long Index, bool Direct);
		delegate void SampleRateChangeCallback(double SampleRate);

		// Super ghetto, but what can we do?
		static BufferSwitchCallback^ OnBufferSwitch = nullptr;
		static SampleRateChangeCallback^ OnSampleRateChange = nullptr;

		property array<Channel^>^ InputChannels
		{ 
			array<Channel^>^ get()
			{
				long count, x;
				Check(m_asio->getChannels(&count, &x));
				return GetChannels(count, true);
			}
		}
		property array<Channel^>^ OutputChannels
		{ 
			array<Channel^>^ get()
			{
				long x, count;
				Check(m_asio->getChannels(&x, &count));
				return GetChannels(count, false);
			}
		}
		
		property long InputLatency 
		{ 
			long get() 
			{
				long input, output;
				Check(m_asio->getLatencies(&input, &output));
				return input;
			}
		}
		property long OutputLatency 
		{ 
			long get() 
			{
				long input, output;
				Check(m_asio->getLatencies(&input, &output));
				return output;
			}
		}
		
		bool IsSampleRateSupported(double SampleRate) { return m_asio->canSampleRate(SampleRate) == ASE_OK; }
		property double SampleRate 
		{ 
			double get() { double rate; Check(m_asio->getSampleRate(&rate)); return rate; } 
			void set(double Rate) { Check(m_asio->setSampleRate(Rate)); }
		}

		void ShowControlPanel() { Check(m_asio->controlPanel()); }
		
		void CreateBuffers(array<Buffers^>^ Inputs, array<Buffers^>^ Outputs, long Size, BufferSwitchCallback^ BufferSwitch, SampleRateChangeCallback^ SampleRateChange)
		{
			if (OnBufferSwitch != nullptr) throw gcnew System::InvalidOperationException("ASIO instance already running");

			OnBufferSwitch = BufferSwitch;
			OnSampleRateChange = SampleRateChange;

			ASIOBufferInfo *infos = new ASIOBufferInfo[Inputs->Length + Outputs->Length];
			ASIOBufferInfo *at = &infos[0];
			for (int i = 0; i < Inputs->Length; ++i, ++at)
			{
				at->isInput = ASIOTrue;
				at->channelNum = Inputs[i]->Channel;
			}
			for (int i = 0; i < Outputs->Length; ++i, ++at)
			{
				at->isInput = ASIOFalse;
				at->channelNum = Outputs[i]->Channel;
			}

			Check(m_asio->createBuffers(infos, Inputs->Length + Outputs->Length, Size, &RouterCallbacks));
			
			at = &infos[0];
			for (int i = 0; i < Inputs->Length; ++i, ++at)
				Inputs[i]->SetBuffers(gcnew array<void *> { at->buffers[0], at->buffers[1] });
			for (int i = 0; i < Outputs->Length; ++i, ++at)
				Outputs[i]->SetBuffers(gcnew array<void *> { at->buffers[0], at->buffers[1] });

			delete[] infos;
		}
		void DisposeBuffers() 
		{ 
			Check(m_asio->disposeBuffers()); 
			OnBufferSwitch = nullptr;
			OnSampleRateChange = nullptr;
		}
		
		bool OutputReady() { return m_asio->outputReady() == ASE_OK; }

		ASIOError getBufferSize(long *minSize, long *maxSize, long *preferredSize, long *granularity) { return m_asio->getBufferSize(minSize, maxSize, preferredSize, granularity); }
		ASIOError getClockSources(ASIOClockSource *clocks, long *numSources) { return m_asio->getClockSources(clocks, numSources); }
		ASIOError setClockSource(long reference) { return m_asio->setClockSource(reference); }
		ASIOError getSamplePosition(ASIOSamples *sPos, ASIOTimeStamp *tStamp) { return m_asio->getSamplePosition(sPos, tStamp); }
		ASIOError future(long selector,void *opt) { return m_asio->future(selector, opt); }
	};
}

void BufferSwitchRouter(long Index, ASIOBool Direct) { AsioWrapper::Asio::OnBufferSwitch(Index, Direct != ASIOFalse); }
void SampleRateChangeRouter(double SampleRate) { AsioWrapper::Asio::OnSampleRateChange(SampleRate); }
//long MessageRouter(long selector, long value, void* message, double* opt) { }
//ASIOTime* BufferSwitchTimeInfoRouter(ASIOTime* params, long doubleBufferIndex, ASIOBool directProcess) { }
