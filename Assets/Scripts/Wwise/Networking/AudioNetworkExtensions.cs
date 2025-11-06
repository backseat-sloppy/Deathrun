using Unity.Netcode;

namespace DeathrunGame.Audio
{
    /// <summary>
    /// Extension methods for network serialization of audio data types.
    /// Provides serialization support for AudioParameterData arrays in RPCs.
    /// </summary>
    public static class AudioNetworkExtensions
    {
        /// <summary>
        /// Write AudioParameterData array to FastBufferWriter.
        /// </summary>
        public static void WriteValueSafe(this FastBufferWriter writer, in AudioParameterData[] value)
        {
            if (value == null)
            {
                writer.WriteValueSafe(0); // Write length as 0 for null arrays
                return;
            }

            writer.WriteValueSafe(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                writer.WriteValueSafe(value[i].parameterName);
                writer.WriteValueSafe(value[i].value);
                writer.WriteValueSafe(value[i].isGlobal);
            }
        }

        /// <summary>
        /// Read AudioParameterData array from FastBufferReader.
        /// </summary>
        public static void ReadValueSafe(this FastBufferReader reader, out AudioParameterData[] value)
        {
            reader.ReadValueSafe(out int length);
            
            if (length == 0)
            {
                value = null;
                return;
            }

            value = new AudioParameterData[length];
            for (int i = 0; i < length; i++)
            {
                reader.ReadValueSafe(out string parameterName);
                reader.ReadValueSafe(out float paramValue);
                reader.ReadValueSafe(out bool isGlobal);
                
                value[i] = new AudioParameterData(parameterName, paramValue, isGlobal);
            }
        }
    }
}