﻿using Drexel.Terminal.Sink;

namespace Drexel.Terminal.Text
{
    /// <summary>
    /// Contains extension methods.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Writes the specified <see cref="Catena"/> <paramref name="catena"/> and advances the cursor.
        /// </summary>
        /// <param name="sink">
        /// The <see cref="ITerminalSink"/> to write to.
        /// </param>
        /// <param name="catena">
        /// The <see cref="Catena"/> to write.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the write operation completed; otherwise, <see langword="false"/>. The most
        /// common reason for an incomplete write operation is if the specified <paramref name="catena"/> extends past
        /// the writeable area of this sink.
        /// </returns>
        public static bool Write(this ITerminalSink sink, Catena catena)
        {
            return sink.Write(catena.ToArray());
        }

        /// <summary>
        /// Writes the specified <see cref="Catena"/> <paramref name="catena"/> starting at the coordinate specified by
        /// the <see cref="Coord"/> <paramref name="destination"/>, if possible. If the write operation completed,
        /// returns <see langword="true"/>; otherwise, returns <see langword="false"/>. The most common reason for an
        /// incomplete write operation is if the specified <paramref name="catena"/> extends past the writeable area of
        /// this sink.
        /// </summary>
        /// <param name="catena">
        /// The <see cref="Catena"/> to write.
        /// </param>
        /// <param name="destination">
        /// The destination to start writing <paramref name="catena"/> from.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the write operation completed; otherwise, <see langword="false"/>. The most
        /// common reason for an incomplete write operation is if the specified <paramref name="catena"/> extends past
        /// the writeable area of this sink.
        /// </returns>
        public static bool Write(this ITerminalSink sink, Catena catena, Coord destination)
        {
            return sink.Write(catena.ToArray(), destination);
        }

        internal static CharInfo[] ToArray(this Catena catena)
        {
            CharInfo[] result = new CharInfo[catena.Value.Length];

            int rangeIndex = 0;
            Range range = catena.Ranges[rangeIndex++];
            for (int index = 0; index < catena.Value.Length; index++)
            {
                if (index >= range.EndIndexExclusive)
                {
                    range = catena.Ranges[rangeIndex++];
                }

                result[index] = new CharInfo(catena.Value[index], range.Colors, range.Delay);
            }

            return result;
        }
    }
}