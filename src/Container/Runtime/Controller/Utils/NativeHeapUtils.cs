using System.Runtime.InteropServices;

namespace Nk7.Container
{
	public static class NativeHeapUtils
    {
        private const int BYTES_COUNT = 1024;
        
		[DllImport("__Internal")]
		private static extern int GC_expand_hp(int bytes);

		/// <summary>
		/// Allocates the spcified amount of memory in the heap for managed objects.
		/// The garbage collecor will not free this memory.
		/// </summary>
		public static void ReserveMemory(int bytes)
		{
#if !UNITY_EDITOR && ENABLE_IL2CPP
			GC_expand_hp(bytes);
#endif
		}

		/// <inheritdoc cref="ReserveMemory(int)"/>
		public static void ReserveMegabytes(int megabytes)
		{
			ReserveMemory(megabytes * BYTES_COUNT * BYTES_COUNT);
		}
	}
}
