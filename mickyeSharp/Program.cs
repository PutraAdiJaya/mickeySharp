using System;
using System.Text;

namespace mickyeSharp
{
    internal class Program
    {
        private static readonly uint[] R_Mask = new uint[4];

        private static readonly uint[] Comp0 = new uint[4];
        private static readonly uint[] Comp1 = new uint[4];

        private static readonly uint[] S_Mask0 = new uint[4];

        private static readonly uint[] S_Mask1 = new uint[4];

        private static void ECRYPT_init()
        {
            /* Initialise the feedback mask associated with register R */
            R_Mask[0] = 0x1279327b;
            R_Mask[1] = 0xb5546660;
            R_Mask[2] = 0xdf87818f;
            R_Mask[3] = 0x00000003;

            /* Initialise Comp0 */
            Comp0[0] = 0x6aa97a30;
            Comp0[1] = 0x7942a809;
            Comp0[2] = 0x057ebfea;
            Comp0[3] = 0x00000006;

            /* Initialise Comp1 */
            Comp1[0] = 0xdd629e9a;
            Comp1[1] = 0xe3a21d63;
            Comp1[2] = 0x91c23dd7;
            Comp1[3] = 0x00000001;

            /* Initialise the feedback masks associated with register S */
            S_Mask0[0] = 0x9ffa7faf;
            S_Mask0[1] = 0xaf4a9381;
            S_Mask0[2] = 0x9cec5802;
            S_Mask0[3] = 0x00000001;

            S_Mask1[0] = 0x4c8cb877;
            S_Mask1[1] = 0x4911b063;
            S_Mask1[2] = 0x40fbc52b;
            S_Mask1[3] = 0x00000008;
        }


        public static void CLOCK_R(ECRYPT_ctx ctx, long input_bit, int control_bit)
        {
            int Feedback_bit;
            /* r_99 ^ input bit */
            int Carry0;
            int Carry1;
            int Carry2;
            /* Respectively, carry from R[0] into R[1], carry from R[1] into R[2] and carry from R[2] into R[3] */

            /* Initialise the variables */
            Feedback_bit = (int) (((ctx.R[3] >> 3) & 1) ^ input_bit);
            Carry0 = (int) ((ctx.R[0] >> 31) & 1);
            Carry1 = (int) ((ctx.R[1] >> 31) & 1);
            Carry2 = (int) ((ctx.R[2] >> 31) & 1);

            if (control_bit != 0)
            {
                /* Shift and xor */
                ctx.R[0] ^= ctx.R[0] << 1;
                ctx.R[1] ^= (uint) ((ctx.R[1] << 1) ^ Carry0);
                ctx.R[2] ^= (uint) ((ctx.R[2] << 1) ^ Carry1);
                ctx.R[3] ^= (uint) ((ctx.R[3] << 1) ^ Carry2);
            }
            else
            {
                /* Shift only */
                ctx.R[0] = ctx.R[0] << 1;
                ctx.R[1] = (uint) ((ctx.R[1] << 1) ^ Carry0);
                ctx.R[2] = (uint) ((ctx.R[2] << 1) ^ Carry1);
                ctx.R[3] = (uint) ((ctx.R[3] << 1) ^ Carry2);
            }

            /* Implement feedback into the various register stages */
            if (Feedback_bit != 0)
            {
                ctx.R[0] ^= R_Mask[0];
                ctx.R[1] ^= R_Mask[1];
                ctx.R[2] ^= R_Mask[2];
                ctx.R[3] ^= R_Mask[3];
            }
        }

        private static void CLOCK_S(ECRYPT_ctx ctx, int input_bit, int control_bit)
        {
            int Feedback_bit;
            /* s_99 ^ input bit */
            int Carry0;
            int Carry1;
            int Carry2;
            /* Respectively, carry from S[0] into S[1], carry from S[1] into S[2] and carry from S[2] into S[3] */

            /* Compute the feedback and carry bits */
            Feedback_bit = (int) (((ctx.S[3] >> 3) & 1) ^ input_bit);
            Carry0 = (int) ((ctx.S[0] >> 31) & 1);
            Carry1 = (int) ((ctx.S[1] >> 31) & 1);
            Carry2 = (int) ((ctx.S[2] >> 31) & 1);

            /* Derive "s hat" according to the MICKEY v 2 specification */
            ctx.S[0] = (ctx.S[0] << 1) ^
                       ((ctx.S[0] ^ Comp0[0]) & ((ctx.S[0] >> 1) ^ (ctx.S[1] << 31) ^ Comp1[0]) & 0xfffffffe);
            ctx.S[1] = (uint) ((ctx.S[1] << 1) ^
                               ((ctx.S[1] ^ Comp0[1]) & ((ctx.S[1] >> 1) ^ (ctx.S[2] << 31) ^ Comp1[1])) ^ Carry0);
            ctx.S[2] = (uint) ((ctx.S[2] << 1) ^
                               ((ctx.S[2] ^ Comp0[2]) & ((ctx.S[2] >> 1) ^ (ctx.S[3] << 31) ^ Comp1[2])) ^ Carry1);
            ctx.S[3] = (uint) ((ctx.S[3] << 1) ^ ((ctx.S[3] ^ Comp0[3]) & ((ctx.S[3] >> 1) ^ Comp1[3]) & 0x7) ^ Carry2);

            /* Apply suitable feedback from s_99 */
            if (Feedback_bit != 0)
            {
                if (control_bit != 0)
                {
                    ctx.S[0] ^= S_Mask1[0];
                    ctx.S[1] ^= S_Mask1[1];
                    ctx.S[2] ^= S_Mask1[2];
                    ctx.S[3] ^= S_Mask1[3];
                }
                else
                {
                    ctx.S[0] ^= S_Mask0[0];
                    ctx.S[1] ^= S_Mask0[1];
                    ctx.S[2] ^= S_Mask0[2];
                    ctx.S[3] ^= S_Mask0[3];
                }
            }
        }

        private static int CLOCK_KG(ECRYPT_ctx ctx, int mixing, int input_bit)
        {
            int Keystream_bit;
            /* Keystream bit to be returned (only valid if mixing = 0 and input_bit = 0 */
            int control_bit_r;
            /* The control bit for register R */
            int control_bit_s;
            /* The control bit for register S */

            Keystream_bit = (int) ((ctx.R[0] ^ ctx.S[0]) & 1);
            control_bit_r = (int) (((ctx.S[1] >> 2) ^ (ctx.R[2] >> 3)) & 1);
            control_bit_s = (int) (((ctx.R[1] >> 1) ^ (ctx.S[2] >> 3)) & 1);

            if (mixing != 0)
                CLOCK_R(ctx, ((ctx.S[1] >> 18) & 1) ^ input_bit, control_bit_r);
            else
                CLOCK_R(ctx, input_bit, control_bit_r);

            CLOCK_S(ctx, input_bit, control_bit_s);

            return Keystream_bit;
        }


        private static void ECRYPT_keysetup(ECRYPT_ctx ctx, uint[] key, uint keysize, uint ivsize) // IV size in bits.
        {
            int i;
            /* Indexing variable */

            /* Store the key in the algorithm context */
            for (i = 0; i < 10; i++) ctx.key[i] = key[i];

            /* Store the iv size in the context too */
            ctx.ivsize = (int) ivsize;
        }

        private static void ECRYPT_ivsetup(ECRYPT_ctx ctx, uint[] iv)
        {
            int i;
            /* Counting/indexing variable */
            int iv_or_key_bit;
            /* Bit being loaded */


            /* Initialise R and S to all zeros */
            for (i = 0; i < 4; i++)
            {
                ctx.R[i] = 0;
                ctx.S[i] = 0;
            }

            /* Load in IV */
            for (i = 0; i < ctx.ivsize; i++)
            {
                iv_or_key_bit = (int) ((iv[i / 8] >> (7 - i % 8)) & 1); // Adopt usual, perverse, labelling order
                CLOCK_KG(ctx, 1, iv_or_key_bit);
            }

            /* Load in K */
            for (i = 0; i < 80; i++)
            {
                iv_or_key_bit = (int) ((ctx.key[i / 8] >> (7 - i % 8)) & 1); // Adopt usual, perverse, labelling order
                CLOCK_KG(ctx, 1, iv_or_key_bit);
            }

            /* Preclock */
            for (i = 0; i < 100; i++) CLOCK_KG(ctx, 1, 0);
        }

        private void
            ECRYPT_process_bytes(int action, ECRYPT_ctx ctx, uint[] input, uint[] output,
                uint msglen) // length in bytes
        {
            uint i;
            uint j;
            /* Counting variables */

            for (i = 0; i < msglen; i++)
            {
                output[i] = input[i];

                for (j = 0; j < 8; j++) output[i] ^= (uint) CLOCK_KG(ctx, 0, 0) << (int) (7 - j);
            }
        }


        private static void
            ECRYPT_keystream_bytes(ECRYPT_ctx ctx, uint[] keystream, uint length) // Length of keystream in bytes.
        {
            uint i;
            uint j;
            /* Counting variables */

            for (i = 0; i < length; i++)
            {
                keystream[i] = 0;

                for (j = 0; j < 8; j++) keystream[i] ^= (uint) CLOCK_KG(ctx, 0, 0) << (int) (7 - j);
            }
        }

        private void perform_iterated_test(uint[] key)
        {
            var ctx = new ECRYPT_ctx();
            /* Keystream generator context */
            var iv = new uint[4];
            /* Array to contain iv derived from keystream */
            var keystream = new uint[16];
            /* Array to contain generated keystream bytes */
            int i;
            /* Counting variable */

            /* Display the key */
            Console.Write("Iterated test key =");
            for (i = 0; i < 10; i++) Console.Write(" {0:x2}", key[i]);

            Console.Write("\n");

            /* Load key */
            ECRYPT_keysetup(ctx, key, 80, 0);
            ECRYPT_ivsetup(ctx, iv);

            for (i = 0; i < 1000; i++)
            {
                /* Generate new key and iv from keystream */
                ECRYPT_keystream_bytes(ctx, key, 10);
                ECRYPT_keystream_bytes(ctx, iv, 4);

                /* Load new key */
                ECRYPT_keysetup(ctx, key, 80, 32);

                /* Load new IV */
                ECRYPT_ivsetup(ctx, iv);
            }

            /* Generate keystream */
            ECRYPT_keystream_bytes(ctx, keystream, 16);

            /* Display the derived keytream */
            Console.Write("Final keystream   =");
            for (i = 0; i < 16; i++) Console.Write(" {0:x2}", keystream[i]);

            Console.Write("\n");

            Console.Write("\n");
        }
        // TAMPILKAN BYTE DLAM BENTUK HEXA
        public static void printBytes(uint[] data)
        {
            for (int i = 0, loopTo = data.Length - 1; i <= loopTo; i++)
                Console.Write("0x{0:X2} ", data[i]);
            Console.WriteLine();
            Console.WriteLine();
        }
        
        // PROSES GRAIN PADA DATA BYTE // XOR KEY DGN DATA
        public static uint[] processBytes(uint[] inS, int inOff, int len, uint[] outS, int outOff,
            uint[] KeyStream)
        {
            // PROSES XOR INPUT DAN KEY
            for (int i = 0, loopTo = len - 1; i <= loopTo; i++)
                outS[outOff + i] = (uint) (inS[inOff + i] ^ KeyStream[i % KeyStream.Length]);
            return outS;
        }
        private static void perform_test(uint[] key, uint[] iv, uint iv_length_in_bits)
        {
            var ctx = new ECRYPT_ctx();
            string PESAN = "ABCD1234~!@#$%^&";
            string KEY = "KEY123";
            uint[] data = Array.ConvertAll(Encoding.ASCII.GetBytes(PESAN), q => Convert.ToUInt32(q));

            Console.WriteLine("===================== DATA =====================");
            printBytes(data);
            
            /* Keystream generator context */
            var keystream = new uint[16];
            /* Array to contain generated keystream bytes */
            int i;
            /* Counting variable */

            /* Load key */
            ECRYPT_keysetup(ctx, key, 80, iv_length_in_bits);
            /* Load IV */
            ECRYPT_ivsetup(ctx, iv);
            /* Generate keystream */
            ECRYPT_keystream_bytes(ctx, keystream, 16);

            /* Display the key */
            Console.WriteLine("===================== KEY =====================");
            printBytes(key);
             

            /* Display the IV */
            Console.WriteLine("===================== IV =====================");
            printBytes(iv);
           
            /* Display the derived keytream */
            
            Console.WriteLine("===================== KEYSTREAM =====================");
            printBytes(keystream);
 
            
            
            uint[] cipher = new uint[data.Length];
            uint[] clear = new uint[data.Length];

            cipher = processBytes(data, 0, data.Length, cipher, 0, keystream);
            Console.WriteLine("===================== CIPER TEXT =====================");
            printBytes(cipher);


            clear = processBytes(cipher, 0, data.Length, clear, 0, keystream);
            Console.WriteLine("===================== DATA RESULT =====================");
            printBytes(clear);
            
            
        }


        private static void Main(string[] argvx)
        {
            var str1 = new string(new char[20]);
            var str2 = new string(new char[16]);

            uint[] key_1 = {0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0, 0x12, 0x34};
            uint[] iv_1 = {0x21, 0x43, 0x65, 0x87};
 
            ECRYPT_init(); 
            perform_test(key_1, iv_1, 32);

            Console.ReadKey(true);
        }

        public class ECRYPT_ctx
        {
 
            public int ivsize;
            public uint[] key = new uint[10];
            public uint[] R = new uint[4];
            public uint[] S = new uint[4];
           
        }
    }
}