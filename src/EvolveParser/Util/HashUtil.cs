namespace EvolveUI {

    internal static unsafe class HashUtil {

        public static int Combine(int h0, int h1) {
            unchecked {
                int hash = 17;
                hash = hash * 31 + h0;
                hash = hash * 31 + h1;
                return hash;
            }
        }

        // this is the standard C# string hash function but extracted to work on a char * instead
        // https://referencesource.microsoft.com/#mscorlib/system/string.cs,0a17bbac4851d0d4,references
        public static int StringHash(char* cbuffer, int size) {
            // Contract.Assert(src[this.Length] == '\0', "src[this.Length] == '\\0'");
            // Contract.Assert( ((int)src)%4 == 0, "Managed string should start at 4 bytes boundary");

            int hash1 = 5381;
            int hash2 = hash1;
            int cnt = 0;
            // todo -- could be improved to remove the branch 
            while (cnt < size) {
                // I reworked this to handle size checks for unaligned char* 
                hash1 = ((hash1 << 5) + hash1) ^ cbuffer[cnt++];
                if (cnt == size) break;
                hash2 = ((hash2 << 5) + hash2) ^ cbuffer[cnt++];
            }

            // while (cnt < size) {
            //     int c = s[0];
            //     hash1 = ((hash1 << 5) + hash1) ^ c;
            //     c = s[1];
            //     if (c == 0)
            //         break;
            //     hash2 = ((hash2 << 5) + hash2) ^ c;
            //     s += 2;
            //     cnt += 2;
            // }

            return hash1 + (hash2 * 1566083941);
        }

        public static int StringHash(string str) {
            fixed (char* cbuffer = str) {
                return StringHash(cbuffer, str.Length);
            }
        }

        public static int Combine(int a, int b, int c) {
            int h = Combine(a, b);
            return Combine(h, c);
        }

        public static int Combine(int a, int b, int c, int d) {
            int h0 = Combine(a, b);
            int h1 = Combine(c, d);
            return Combine(h0, h1);
        }

        // https://stackoverflow.com/questions/664014/what-integer-hash-function-are-good-that-accepts-an-integer-hash-key
        public static int HashInteger(int x) {
            x = ((x >> 16) ^ x) * 0x45d9f3b;
            x = ((x >> 16) ^ x) * 0x45d9f3b;
            x = (x >> 16) ^ x;
            return x;
        }

        public static int UnhashInteger(int x) {
            x = ((x >> 16) ^ x) * 0x119de1f3;
            x = ((x >> 16) ^ x) * 0x119de1f3;
            x = (x >> 16) ^ x;
            return x;
        }

    }

}