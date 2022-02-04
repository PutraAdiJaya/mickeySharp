Imports System.Text

Friend Class Program
	Private Shared ReadOnly R_Mask As UInteger() = New UInteger(3) {}

	Private Shared ReadOnly Comp0 As UInteger() = New UInteger(3) {}
	Private Shared ReadOnly Comp1 As UInteger() = New UInteger(3) {}

	Private Shared ReadOnly S_Mask0 As UInteger() = New UInteger(3) {}

	Private Shared ReadOnly S_Mask1 As UInteger() = New UInteger(3) {}

	Private Shared Sub ECRYPT_init()
		' Initialise the feedback mask associated with register R 

		R_Mask(0) = &H1279327b
		R_Mask(1) = &Hb5546660UI
		R_Mask(2) = &Hdf87818fUI
		R_Mask(3) = &H3

		' Initialise Comp0 

		Comp0(0) = &H6aa97a30
		Comp0(1) = &H7942a809
		Comp0(2) = &H57ebfea
		Comp0(3) = &H6

		' Initialise Comp1 

		Comp1(0) = &Hdd629e9aUI
		Comp1(1) = &He3a21d63UI
		Comp1(2) = &H91c23dd7UI
		Comp1(3) = &H1

		' Initialise the feedback masks associated with register S 

		S_Mask0(0) = &H9ffa7fafUI
		S_Mask0(1) = &Haf4a9381UI
		S_Mask0(2) = &H9cec5802UI
		S_Mask0(3) = &H1

		S_Mask1(0) = &H4c8cb877
		S_Mask1(1) = &H4911b063
		S_Mask1(2) = &H40fbc52b
		S_Mask1(3) = &H8
	End Sub


	Public Shared Sub CLOCK_R(ctx As ECRYPT_ctx, input_bit As Long, control_bit As Integer)
		Dim Feedback_bit As Integer
		' r_99 ^ input bit 

		Dim Carry0 As Integer
		Dim Carry1 As Integer
		Dim Carry2 As Integer
		' Respectively, carry from R[0] into R[1], carry from R[1] into R[2] and carry from R[2] into R[3] 


		' Initialise the variables 

		Feedback_bit = CInt(((ctx.R(3) >> 3) And 1) Xor input_bit)
		Carry0 = CInt((ctx.R(0) >> 31) And 1)
		Carry1 = CInt((ctx.R(1) >> 31) And 1)
		Carry2 = CInt((ctx.R(2) >> 31) And 1)

		If control_bit <> 0 Then
			' Shift and xor 

			ctx.R(0) = ctx.R(0) Xor ctx.R(0) << 1
			ctx.R(1) = ctx.R(1) Xor CUInt((ctx.R(1) << 1) Xor Carry0)
			ctx.R(2) = ctx.R(2) Xor CUInt((ctx.R(2) << 1) Xor Carry1)
			ctx.R(3) = ctx.R(3) Xor CUInt((ctx.R(3) << 1) Xor Carry2)
		Else
			' Shift only 

			ctx.R(0) = ctx.R(0) << 1
			ctx.R(1) = CUInt((ctx.R(1) << 1) Xor Carry0)
			ctx.R(2) = CUInt((ctx.R(2) << 1) Xor Carry1)
			ctx.R(3) = CUInt((ctx.R(3) << 1) Xor Carry2)
		End If

		' Implement feedback into the various register stages 

		If Feedback_bit <> 0 Then
			ctx.R(0) = ctx.R(0) Xor R_Mask(0)
			ctx.R(1) = ctx.R(1) Xor R_Mask(1)
			ctx.R(2) = ctx.R(2) Xor R_Mask(2)
			ctx.R(3) = ctx.R(3) Xor R_Mask(3)
		End If
	End Sub

	Private Shared Sub CLOCK_S(ctx As ECRYPT_ctx, input_bit As Integer, control_bit As Integer)
		Dim Feedback_bit As Integer
		' s_99 ^ input bit 

		Dim Carry0 As Integer
		Dim Carry1 As Integer
		Dim Carry2 As Integer
		' Respectively, carry from S[0] into S[1], carry from S[1] into S[2] and carry from S[2] into S[3] 


		' Compute the feedback and carry bits 

		Feedback_bit = CInt(((ctx.S(3) >> 3) And 1) Xor input_bit)
		Carry0 = CInt((ctx.S(0) >> 31) And 1)
		Carry1 = CInt((ctx.S(1) >> 31) And 1)
		Carry2 = CInt((ctx.S(2) >> 31) And 1)

		' Derive "s hat" according to the MICKEY v 2 specification 

		ctx.S(0) = (ctx.S(0) << 1) Xor ((ctx.S(0) Xor Comp0(0)) And ((ctx.S(0) >> 1) Xor (ctx.S(1) << 31) Xor Comp1(0)) And &HfffffffeUI)
		ctx.S(1) = CUInt((ctx.S(1) << 1) Xor ((ctx.S(1) Xor Comp0(1)) And ((ctx.S(1) >> 1) Xor (ctx.S(2) << 31) Xor Comp1(1))) Xor Carry0)
		ctx.S(2) = CUInt((ctx.S(2) << 1) Xor ((ctx.S(2) Xor Comp0(2)) And ((ctx.S(2) >> 1) Xor (ctx.S(3) << 31) Xor Comp1(2))) Xor Carry1)
		ctx.S(3) = CUInt((ctx.S(3) << 1) Xor ((ctx.S(3) Xor Comp0(3)) And ((ctx.S(3) >> 1) Xor Comp1(3)) And &H7) Xor Carry2)

		' Apply suitable feedback from s_99 

		If Feedback_bit <> 0 Then
			If control_bit <> 0 Then
				ctx.S(0) = ctx.S(0) Xor S_Mask1(0)
				ctx.S(1) = ctx.S(1) Xor S_Mask1(1)
				ctx.S(2) = ctx.S(2) Xor S_Mask1(2)
				ctx.S(3) = ctx.S(3) Xor S_Mask1(3)
			Else
				ctx.S(0) = ctx.S(0) Xor S_Mask0(0)
				ctx.S(1) = ctx.S(1) Xor S_Mask0(1)
				ctx.S(2) = ctx.S(2) Xor S_Mask0(2)
				ctx.S(3) = ctx.S(3) Xor S_Mask0(3)
			End If
		End If
	End Sub

	Private Shared Function CLOCK_KG(ctx As ECRYPT_ctx, mixing As Integer, input_bit As Integer) As Integer
		Dim Keystream_bit As Integer
		' Keystream bit to be returned (only valid if mixing = 0 and input_bit = 0 

		Dim control_bit_r As Integer
		' The control bit for register R 

		Dim control_bit_s As Integer
		' The control bit for register S 


		Keystream_bit = CInt((ctx.R(0) Xor ctx.S(0)) And 1)
		control_bit_r = CInt(((ctx.S(1) >> 2) Xor (ctx.R(2) >> 3)) And 1)
		control_bit_s = CInt(((ctx.R(1) >> 1) Xor (ctx.S(2) >> 3)) And 1)

		If mixing <> 0 Then
			CLOCK_R(ctx, ((ctx.S(1) >> 18) And 1) Xor input_bit, control_bit_r)
		Else
			CLOCK_R(ctx, input_bit, control_bit_r)
		End If

		CLOCK_S(ctx, input_bit, control_bit_s)

		Return Keystream_bit
	End Function


	Private Shared Sub ECRYPT_keysetup(ctx As ECRYPT_ctx, key As UInteger(), keysize As UInteger, ivsize As UInteger)
	' IV size in bits.
		Dim i As Integer
		' Indexing variable 


		' Store the key in the algorithm context 

		For i = 0 To 9
			ctx.key(i) = key(i)
		Next

		' Store the iv size in the context too 

		ctx.ivsize = CInt(ivsize)
	End Sub

	Private Shared Sub ECRYPT_ivsetup(ctx As ECRYPT_ctx, iv As UInteger())
		Dim i As Integer
		' Counting/indexing variable 

		Dim iv_or_key_bit As Integer
		' Bit being loaded 



		' Initialise R and S to all zeros 

		For i = 0 To 3
			ctx.R(i) = 0
			ctx.S(i) = 0
		Next

		' Load in IV 

		For i = 0 To ctx.ivsize - 1
			iv_or_key_bit = CInt((iv(i \ 8) >> (7 - i Mod 8)) And 1)
			' Adopt usual, perverse, labelling order
			CLOCK_KG(ctx, 1, iv_or_key_bit)
		Next

		' Load in K 

		For i = 0 To 79
			iv_or_key_bit = CInt((ctx.key(i \ 8) >> (7 - i Mod 8)) And 1)
			' Adopt usual, perverse, labelling order
			CLOCK_KG(ctx, 1, iv_or_key_bit)
		Next

		' Preclock 

		For i = 0 To 99
			CLOCK_KG(ctx, 1, 0)
		Next
	End Sub

	Private Sub ECRYPT_process_bytes(action As Integer, ctx As ECRYPT_ctx, input As UInteger(), output As UInteger(), msglen As UInteger)
	' length in bytes
		Dim i As UInteger
		Dim j As UInteger
		' Counting variables 


		For i = 0 To msglen - 1
			output(i) = input(i)

			For j = 0 To 7
				output(i) = output(i) Xor CUInt(CLOCK_KG(ctx, 0, 0)) << CInt(7 - j)
			Next
		Next
	End Sub


	Private Shared Sub ECRYPT_keystream_bytes(ctx As ECRYPT_ctx, keystream As UInteger(), length As UInteger)
	' Length of keystream in bytes.
		Dim i As UInteger
		Dim j As UInteger
		' Counting variables 


		For i = 0 To length - 1
			keystream(i) = 0

			For j = 0 To 7
				keystream(i) = keystream(i) Xor CUInt(CLOCK_KG(ctx, 0, 0)) << CInt(7 - j)
			Next
		Next
	End Sub

	Private Sub perform_iterated_test(key As UInteger())
		Dim ctx = New ECRYPT_ctx()
		' Keystream generator context 

		Dim iv = New UInteger(3) {}
		' Array to contain iv derived from keystream 

		Dim keystream = New UInteger(15) {}
		' Array to contain generated keystream bytes 

		Dim i As Integer
		' Counting variable 


		' Display the key 

		Console.Write("Iterated test key =")
		For i = 0 To 9
			Console.Write(" {0:x2}", key(i))
		Next

		Console.Write(vbLf)

		' Load key 

		ECRYPT_keysetup(ctx, key, 80, 0)
		ECRYPT_ivsetup(ctx, iv)

		For i = 0 To 999
			' Generate new key and iv from keystream 

			ECRYPT_keystream_bytes(ctx, key, 10)
			ECRYPT_keystream_bytes(ctx, iv, 4)

			' Load new key 

			ECRYPT_keysetup(ctx, key, 80, 32)

			' Load new IV 

			ECRYPT_ivsetup(ctx, iv)
		Next

		' Generate keystream 

		ECRYPT_keystream_bytes(ctx, keystream, 16)

		' Display the derived keytream 

		Console.Write("Final keystream   =")
		For i = 0 To 15
			Console.Write(" {0:x2}", keystream(i))
		Next

		Console.Write(vbLf)

		Console.Write(vbLf)
	End Sub
	' TAMPILKAN BYTE DLAM BENTUK HEXA
	Public Shared Sub printBytes(data As UInteger())
		Dim i As Integer = 0, loopTo As Integer = data.Length - 1
		While i <= loopTo
			Console.Write("0x{0:X2} ", data(i))
			i += 1
		End While
		Console.WriteLine()
		Console.WriteLine()
	End Sub

	' PROSES GRAIN PADA DATA BYTE // XOR KEY DGN DATA
	Public Shared Function processBytes(inS As UInteger(), inOff As Integer, len As Integer, outS As UInteger(), outOff As Integer, KeyStream As UInteger()) As UInteger()
		' PROSES XOR INPUT DAN KEY
		Dim i As Integer = 0, loopTo As Integer = len - 1
		While i <= loopTo
			outS(outOff + i) = CUInt(inS(inOff + i) Xor KeyStream(i Mod KeyStream.Length))
			i += 1
		End While
		Return outS
	End Function
	Private Shared Sub perform_test(key__1 As UInteger(), iv As UInteger(), iv_length_in_bits As UInteger)
		Dim ctx = New ECRYPT_ctx()
		Dim PESAN As String = "ABCD1234~!@#$%^&"
		Dim KEY__2 As String = "KEY123"
		Dim data As UInteger() = Array.ConvertAll(Encoding.ASCII.GetBytes(PESAN), Function(q) Convert.ToUInt32(q))

		Console.WriteLine("===================== DATA =====================")
		printBytes(data)

		' Keystream generator context 

		Dim keystream = New UInteger(15) {}
		' Array to contain generated keystream bytes 

		Dim i As Integer
		' Counting variable 


		' Load key 

		ECRYPT_keysetup(ctx, key__1, 80, iv_length_in_bits)
		' Load IV 

		ECRYPT_ivsetup(ctx, iv)
		' Generate keystream 

		ECRYPT_keystream_bytes(ctx, keystream, 16)

		' Display the key 

		Console.WriteLine("===================== KEY =====================")
		printBytes(key__1)


		' Display the IV 

		Console.WriteLine("===================== IV =====================")
		printBytes(iv)

		' Display the derived keytream 


		Console.WriteLine("===================== KEYSTREAM =====================")
		printBytes(keystream)



		Dim cipher As UInteger() = New UInteger(data.Length - 1) {}
		Dim clear As UInteger() = New UInteger(data.Length - 1) {}

		cipher = processBytes(data, 0, data.Length, cipher, 0, keystream)
		Console.WriteLine("===================== CIPER TEXT =====================")
		printBytes(cipher)


		clear = processBytes(cipher, 0, data.Length, clear, 0, keystream)
		Console.WriteLine("===================== DATA RESULT =====================")
		printBytes(clear)


	End Sub


	Friend Shared Sub Main(argvx As String())
		Dim str1 = New String(New Char(19) {})
		Dim str2 = New String(New Char(15) {})

		Dim key_1 As UInteger() = {&H12, &H34, &H56, &H78, &H9a, &Hbc, _
			&Hde, &Hf0, &H12, &H34}
		Dim iv_1 As UInteger() = {&H21, &H43, &H65, &H87}

		ECRYPT_init()
		perform_test(key_1, iv_1, 32)

		Console.ReadKey(True)
	End Sub

	Public Class ECRYPT_ctx

		Public ivsize As Integer
		Public key As UInteger() = New UInteger(9) {}
		Public R As UInteger() = New UInteger(3) {}
		Public S As UInteger() = New UInteger(3) {}

	End Class
End Class
