using System;
using System.IO;
using System.Text;

namespace Circuit.Headless
{
    internal sealed class WaveData
    {
        public WaveData(double[] samples, int sampleRate)
        {
            Samples = samples ?? throw new ArgumentNullException(nameof(samples));
            SampleRate = sampleRate;
        }

        public double[] Samples { get; }

        public int SampleRate { get; }
    }

    internal static class WaveFile
    {
        public static WaveData ReadMono(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: false);

            if (ReadFourCc(reader) != "RIFF")
                throw new InvalidDataException("WAV file is missing a RIFF header.");

            reader.ReadInt32();

            if (ReadFourCc(reader) != "WAVE")
                throw new InvalidDataException("File is not a WAVE container.");

            ushort audioFormat = 0;
            ushort channels = 0;
            int sampleRate = 0;
            ushort bitsPerSample = 0;
            byte[] data = null;

            while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
            {
                string chunkId = ReadFourCc(reader);
                int chunkSize = reader.ReadInt32();
                long nextChunk = reader.BaseStream.Position + chunkSize + (chunkSize % 2);

                if (chunkId == "fmt ")
                {
                    audioFormat = reader.ReadUInt16();
                    channels = reader.ReadUInt16();
                    sampleRate = reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadUInt16();
                    bitsPerSample = reader.ReadUInt16();
                }
                else if (chunkId == "data")
                {
                    data = reader.ReadBytes(chunkSize);
                }

                reader.BaseStream.Position = nextChunk;
            }

            if (channels == 0 || sampleRate <= 0 || bitsPerSample == 0 || data == null)
                throw new InvalidDataException("WAV file is missing required format or data chunks.");

            int bytesPerSample = bitsPerSample / 8;
            int frameSize = bytesPerSample * channels;
            if (bytesPerSample == 0 || frameSize == 0 || data.Length % frameSize != 0)
                throw new InvalidDataException("WAV file has an unsupported frame layout.");

            int frameCount = data.Length / frameSize;
            double[] samples = new double[frameCount];

            for (int frame = 0; frame < frameCount; frame++)
            {
                double mixed = 0;
                for (int channel = 0; channel < channels; channel++)
                {
                    int offset = frame * frameSize + channel * bytesPerSample;
                    mixed += DecodeSample(data, offset, audioFormat, bitsPerSample);
                }

                samples[frame] = mixed / channels;
            }

            return new WaveData(samples, sampleRate);
        }

        public static void WriteMono16(string path, double[] samples, int sampleRate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

            using FileStream stream = File.Create(path);
            using BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: false);

            const short channels = 1;
            const short bitsPerSample = 16;
            int bytesPerSample = bitsPerSample / 8;
            int dataSize = samples.Length * bytesPerSample;
            int byteRate = sampleRate * channels * bytesPerSample;
            short blockAlign = (short)(channels * bytesPerSample);

            WriteFourCc(writer, "RIFF");
            writer.Write(36 + dataSize);
            WriteFourCc(writer, "WAVE");

            WriteFourCc(writer, "fmt ");
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);

            WriteFourCc(writer, "data");
            writer.Write(dataSize);

            foreach (double sample in samples)
            {
                double clipped = Math.Max(-1.0, Math.Min(1.0, sample));
                short pcm = (short)Math.Round(clipped * short.MaxValue);
                writer.Write(pcm);
            }
        }

        private static double DecodeSample(byte[] data, int offset, ushort audioFormat, ushort bitsPerSample)
        {
            return (audioFormat, bitsPerSample) switch
            {
                (1, 8) => (data[offset] - 128) / 128.0,
                (1, 16) => BitConverter.ToInt16(data, offset) / 32768.0,
                (1, 24) => ReadInt24(data, offset) / 8388608.0,
                (1, 32) => BitConverter.ToInt32(data, offset) / 2147483648.0,
                (3, 32) => BitConverter.ToSingle(data, offset),
                _ => throw new NotSupportedException($"Unsupported WAV format: audioFormat={audioFormat}, bitsPerSample={bitsPerSample}.")
            };
        }

        private static int ReadInt24(byte[] data, int offset)
        {
            int value = data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16);
            if ((value & 0x00800000) != 0)
                value |= unchecked((int)0xFF000000);
            return value;
        }

        private static string ReadFourCc(BinaryReader reader)
        {
            return Encoding.ASCII.GetString(reader.ReadBytes(4));
        }

        private static void WriteFourCc(BinaryWriter writer, string fourCc)
        {
            writer.Write(Encoding.ASCII.GetBytes(fourCc));
        }
    }
}