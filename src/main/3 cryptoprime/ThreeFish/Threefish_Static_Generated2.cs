using System;
// Only encrypt and only for 1024 threefish (useful for OFB or CFB modes)
// Vinogradov S.V. Generated at 2020-2024 years
namespace CodeGenerated.Cryptoprimes
{
	public static unsafe class Threefish_Static_Generated2 
	{
		/// <summary>Step for Threefish1024. DANGER! Tweak contain 3 elements of ulong, not 2!!! (third value is a tweak[0] ^ tweak[1])</summary>
		/// <param name="key">Key for cipher (128 bytes)</param>
		/// <param name="tweak">Tweak for cipher. DANGER! Tweak is a 8*3 bytes, not 8*2!!! (third value is a tweak[0] ^ tweak[1])</param>
		/// <param name="text">Open text for cipher</param>
		public static void Threefish1024_step(byte * key, byte * tweak, byte * text)
		{
			// Aliases
			ref ulong key00   = ref ((ulong *)key)  [00];
			ref ulong text00  = ref ((ulong *)text) [00];
			ref ulong tweak00 = ref ((ulong *)tweak)[00];
			ref ulong key01   = ref ((ulong *)key)  [01];
			ref ulong text01  = ref ((ulong *)text) [01];
			ref ulong tweak01 = ref ((ulong *)tweak)[01];
			ref ulong key02   = ref ((ulong *)key)  [02];
			ref ulong text02  = ref ((ulong *)text) [02];
			ref ulong tweak02 = ref ((ulong *)tweak)[02];
			ref ulong key03   = ref ((ulong *)key)  [03];
			ref ulong text03  = ref ((ulong *)text) [03];
			ref ulong key04   = ref ((ulong *)key)  [04];
			ref ulong text04  = ref ((ulong *)text) [04];
			ref ulong key05   = ref ((ulong *)key)  [05];
			ref ulong text05  = ref ((ulong *)text) [05];
			ref ulong key06   = ref ((ulong *)key)  [06];
			ref ulong text06  = ref ((ulong *)text) [06];
			ref ulong key07   = ref ((ulong *)key)  [07];
			ref ulong text07  = ref ((ulong *)text) [07];
			ref ulong key08   = ref ((ulong *)key)  [08];
			ref ulong text08  = ref ((ulong *)text) [08];
			ref ulong key09   = ref ((ulong *)key)  [09];
			ref ulong text09  = ref ((ulong *)text) [09];
			ref ulong key10   = ref ((ulong *)key)  [10];
			ref ulong text10  = ref ((ulong *)text) [10];
			ref ulong key11   = ref ((ulong *)key)  [11];
			ref ulong text11  = ref ((ulong *)text) [11];
			ref ulong key12   = ref ((ulong *)key)  [12];
			ref ulong text12  = ref ((ulong *)text) [12];
			ref ulong key13   = ref ((ulong *)key)  [13];
			ref ulong text13  = ref ((ulong *)text) [13];
			ref ulong key14   = ref ((ulong *)key)  [14];
			ref ulong text14  = ref ((ulong *)text) [14];
			ref ulong key15   = ref ((ulong *)key)  [15];
			ref ulong text15  = ref ((ulong *)text) [15];
			ref ulong key16   = ref ((ulong *)key)  [16];
			ref ulong text16  = ref ((ulong *)text) [16];
			
			// round 00
			// Mix text00 text01 24
			text01 += key01;
			text00 += text01 + key00;
			text01 = text01 << 24 | text01 >> (64-24);
			text01 ^= text00;
			// Mix text02 text03 13
			text03 += key03;
			text02 += text03 + key02;
			text03 = text03 << 13 | text03 >> (64-13);
			text03 ^= text02;
			// Mix text04 text05 08
			text05 += key05;
			text04 += text05 + key04;
			text05 = text05 << 08 | text05 >> (64-08);
			text05 ^= text04;
			// Mix text06 text07 47
			text07 += key07;
			text06 += text07 + key06;
			text07 = text07 << 47 | text07 >> (64-47);
			text07 ^= text06;
			// Mix text08 text09 08
			text09 += key09;
			text08 += text09 + key08;
			text09 = text09 << 08 | text09 >> (64-08);
			text09 ^= text08;
			// Mix text10 text11 17
			text11 += key11;
			text10 += text11 + key10;
			text11 = text11 << 17 | text11 >> (64-17);
			text11 ^= text10;
			// Mix text12 text13 22
			text13 += key13;
			text12 += text13 + key12;
			text13 = text13 << 22 | text13 >> (64-22);
			text13 ^= text12;
			// Mix text14 text15 37
			text15 += key15 + 00;
			text14 += text15 + key14 + tweak01;
			text15 = text15 << 37 | text15 >> (64-37);
			text15 ^= text14;
			
			// round 01
			// Mix text00 text09 38
			text00 += text09;
			text09 = text09 << 38 | text09 >> (64-38);
			text09 ^= text00;
			// Mix text02 text13 19
			text02 += text13;
			text13 = text13 << 19 | text13 >> (64-19);
			text13 ^= text02;
			// Mix text06 text11 10
			text06 += text11;
			text11 = text11 << 10 | text11 >> (64-10);
			text11 ^= text06;
			// Mix text04 text15 55
			text04 += text15;
			text15 = text15 << 55 | text15 >> (64-55);
			text15 ^= text04;
			// Mix text10 text07 49
			text10 += text07;
			text07 = text07 << 49 | text07 >> (64-49);
			text07 ^= text10;
			// Mix text12 text03 18
			text12 += text03;
			text03 = text03 << 18 | text03 >> (64-18);
			text03 ^= text12;
			// Mix text14 text05 23
			text14 += text05;
			text05 = text05 << 23 | text05 >> (64-23);
			text05 ^= text14;
			// Mix text08 text01 52
			text08 += text01;
			text01 = text01 << 52 | text01 >> (64-52);
			text01 ^= text08;
			
			// round 02
			// Mix text00 text07 33
			text00 += text07;
			text07 = text07 << 33 | text07 >> (64-33);
			text07 ^= text00;
			// Mix text02 text05 04
			text02 += text05;
			text05 = text05 << 04 | text05 >> (64-04);
			text05 ^= text02;
			// Mix text04 text03 51
			text04 += text03;
			text03 = text03 << 51 | text03 >> (64-51);
			text03 ^= text04;
			// Mix text06 text01 13
			text06 += text01;
			text01 = text01 << 13 | text01 >> (64-13);
			text01 ^= text06;
			// Mix text12 text15 34
			text12 += text15;
			text15 = text15 << 34 | text15 >> (64-34);
			text15 ^= text12;
			// Mix text14 text13 41
			text14 += text13;
			text13 = text13 << 41 | text13 >> (64-41);
			text13 ^= text14;
			// Mix text08 text11 59
			text08 += text11;
			text11 = text11 << 59 | text11 >> (64-59);
			text11 ^= text08;
			// Mix text10 text09 17
			text10 += text09;
			text09 = text09 << 17 | text09 >> (64-17);
			text09 ^= text10;
			
			// round 03
			// Mix text00 text15 05
			text00 += text15;
			text15 = text15 << 05 | text15 >> (64-05);
			text15 ^= text00;
			// Mix text02 text11 20
			text02 += text11;
			text11 = text11 << 20 | text11 >> (64-20);
			text11 ^= text02;
			// Mix text06 text13 48
			text06 += text13;
			text13 = text13 << 48 | text13 >> (64-48);
			text13 ^= text06;
			// Mix text04 text09 41
			text04 += text09;
			text09 = text09 << 41 | text09 >> (64-41);
			text09 ^= text04;
			// Mix text14 text01 47
			text14 += text01;
			text01 = text01 << 47 | text01 >> (64-47);
			text01 ^= text14;
			// Mix text08 text05 28
			text08 += text05;
			text05 = text05 << 28 | text05 >> (64-28);
			text05 ^= text08;
			// Mix text10 text03 16
			text10 += text03;
			text03 = text03 << 16 | text03 >> (64-16);
			text03 ^= text10;
			// Mix text12 text07 25
			text12 += text07;
			text07 = text07 << 25 | text07 >> (64-25);
			text07 ^= text12;
			
			// round 04
			// Mix text00 text01 41
			text01 += key02;
			text00 += text01 + key01;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text00;
			// Mix text02 text03 09
			text03 += key04;
			text02 += text03 + key03;
			text03 = text03 << 09 | text03 >> (64-09);
			text03 ^= text02;
			// Mix text04 text05 37
			text05 += key06;
			text04 += text05 + key05;
			text05 = text05 << 37 | text05 >> (64-37);
			text05 ^= text04;
			// Mix text06 text07 31
			text07 += key08;
			text06 += text07 + key07;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text06;
			// Mix text08 text09 12
			text09 += key10;
			text08 += text09 + key09;
			text09 = text09 << 12 | text09 >> (64-12);
			text09 ^= text08;
			// Mix text10 text11 47
			text11 += key12;
			text10 += text11 + key11;
			text11 = text11 << 47 | text11 >> (64-47);
			text11 ^= text10;
			// Mix text12 text13 44
			text13 += key14;
			text12 += text13 + key13;
			text13 = text13 << 44 | text13 >> (64-44);
			text13 ^= text12;
			// Mix text14 text15 30
			text15 += key16 + 01;
			text14 += text15 + key15 + tweak02;
			text15 = text15 << 30 | text15 >> (64-30);
			text15 ^= text14;
			
			// round 05
			// Mix text00 text09 16
			text00 += text09;
			text09 = text09 << 16 | text09 >> (64-16);
			text09 ^= text00;
			// Mix text02 text13 34
			text02 += text13;
			text13 = text13 << 34 | text13 >> (64-34);
			text13 ^= text02;
			// Mix text06 text11 56
			text06 += text11;
			text11 = text11 << 56 | text11 >> (64-56);
			text11 ^= text06;
			// Mix text04 text15 51
			text04 += text15;
			text15 = text15 << 51 | text15 >> (64-51);
			text15 ^= text04;
			// Mix text10 text07 04
			text10 += text07;
			text07 = text07 << 04 | text07 >> (64-04);
			text07 ^= text10;
			// Mix text12 text03 53
			text12 += text03;
			text03 = text03 << 53 | text03 >> (64-53);
			text03 ^= text12;
			// Mix text14 text05 42
			text14 += text05;
			text05 = text05 << 42 | text05 >> (64-42);
			text05 ^= text14;
			// Mix text08 text01 41
			text08 += text01;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text08;
			
			// round 06
			// Mix text00 text07 31
			text00 += text07;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text00;
			// Mix text02 text05 44
			text02 += text05;
			text05 = text05 << 44 | text05 >> (64-44);
			text05 ^= text02;
			// Mix text04 text03 47
			text04 += text03;
			text03 = text03 << 47 | text03 >> (64-47);
			text03 ^= text04;
			// Mix text06 text01 46
			text06 += text01;
			text01 = text01 << 46 | text01 >> (64-46);
			text01 ^= text06;
			// Mix text12 text15 19
			text12 += text15;
			text15 = text15 << 19 | text15 >> (64-19);
			text15 ^= text12;
			// Mix text14 text13 42
			text14 += text13;
			text13 = text13 << 42 | text13 >> (64-42);
			text13 ^= text14;
			// Mix text08 text11 44
			text08 += text11;
			text11 = text11 << 44 | text11 >> (64-44);
			text11 ^= text08;
			// Mix text10 text09 25
			text10 += text09;
			text09 = text09 << 25 | text09 >> (64-25);
			text09 ^= text10;
			
			// round 07
			// Mix text00 text15 09
			text00 += text15;
			text15 = text15 << 09 | text15 >> (64-09);
			text15 ^= text00;
			// Mix text02 text11 48
			text02 += text11;
			text11 = text11 << 48 | text11 >> (64-48);
			text11 ^= text02;
			// Mix text06 text13 35
			text06 += text13;
			text13 = text13 << 35 | text13 >> (64-35);
			text13 ^= text06;
			// Mix text04 text09 52
			text04 += text09;
			text09 = text09 << 52 | text09 >> (64-52);
			text09 ^= text04;
			// Mix text14 text01 23
			text14 += text01;
			text01 = text01 << 23 | text01 >> (64-23);
			text01 ^= text14;
			// Mix text08 text05 31
			text08 += text05;
			text05 = text05 << 31 | text05 >> (64-31);
			text05 ^= text08;
			// Mix text10 text03 37
			text10 += text03;
			text03 = text03 << 37 | text03 >> (64-37);
			text03 ^= text10;
			// Mix text12 text07 20
			text12 += text07;
			text07 = text07 << 20 | text07 >> (64-20);
			text07 ^= text12;
			
			// round 08
			// Mix text00 text01 24
			text01 += key03;
			text00 += text01 + key02;
			text01 = text01 << 24 | text01 >> (64-24);
			text01 ^= text00;
			// Mix text02 text03 13
			text03 += key05;
			text02 += text03 + key04;
			text03 = text03 << 13 | text03 >> (64-13);
			text03 ^= text02;
			// Mix text04 text05 08
			text05 += key07;
			text04 += text05 + key06;
			text05 = text05 << 08 | text05 >> (64-08);
			text05 ^= text04;
			// Mix text06 text07 47
			text07 += key09;
			text06 += text07 + key08;
			text07 = text07 << 47 | text07 >> (64-47);
			text07 ^= text06;
			// Mix text08 text09 08
			text09 += key11;
			text08 += text09 + key10;
			text09 = text09 << 08 | text09 >> (64-08);
			text09 ^= text08;
			// Mix text10 text11 17
			text11 += key13;
			text10 += text11 + key12;
			text11 = text11 << 17 | text11 >> (64-17);
			text11 ^= text10;
			// Mix text12 text13 22
			text13 += key15;
			text12 += text13 + key14;
			text13 = text13 << 22 | text13 >> (64-22);
			text13 ^= text12;
			// Mix text14 text15 37
			text15 += key00 + 02;
			text14 += text15 + key16 + tweak00;
			text15 = text15 << 37 | text15 >> (64-37);
			text15 ^= text14;
			
			// round 09
			// Mix text00 text09 38
			text00 += text09;
			text09 = text09 << 38 | text09 >> (64-38);
			text09 ^= text00;
			// Mix text02 text13 19
			text02 += text13;
			text13 = text13 << 19 | text13 >> (64-19);
			text13 ^= text02;
			// Mix text06 text11 10
			text06 += text11;
			text11 = text11 << 10 | text11 >> (64-10);
			text11 ^= text06;
			// Mix text04 text15 55
			text04 += text15;
			text15 = text15 << 55 | text15 >> (64-55);
			text15 ^= text04;
			// Mix text10 text07 49
			text10 += text07;
			text07 = text07 << 49 | text07 >> (64-49);
			text07 ^= text10;
			// Mix text12 text03 18
			text12 += text03;
			text03 = text03 << 18 | text03 >> (64-18);
			text03 ^= text12;
			// Mix text14 text05 23
			text14 += text05;
			text05 = text05 << 23 | text05 >> (64-23);
			text05 ^= text14;
			// Mix text08 text01 52
			text08 += text01;
			text01 = text01 << 52 | text01 >> (64-52);
			text01 ^= text08;
			
			// round 10
			// Mix text00 text07 33
			text00 += text07;
			text07 = text07 << 33 | text07 >> (64-33);
			text07 ^= text00;
			// Mix text02 text05 04
			text02 += text05;
			text05 = text05 << 04 | text05 >> (64-04);
			text05 ^= text02;
			// Mix text04 text03 51
			text04 += text03;
			text03 = text03 << 51 | text03 >> (64-51);
			text03 ^= text04;
			// Mix text06 text01 13
			text06 += text01;
			text01 = text01 << 13 | text01 >> (64-13);
			text01 ^= text06;
			// Mix text12 text15 34
			text12 += text15;
			text15 = text15 << 34 | text15 >> (64-34);
			text15 ^= text12;
			// Mix text14 text13 41
			text14 += text13;
			text13 = text13 << 41 | text13 >> (64-41);
			text13 ^= text14;
			// Mix text08 text11 59
			text08 += text11;
			text11 = text11 << 59 | text11 >> (64-59);
			text11 ^= text08;
			// Mix text10 text09 17
			text10 += text09;
			text09 = text09 << 17 | text09 >> (64-17);
			text09 ^= text10;
			
			// round 11
			// Mix text00 text15 05
			text00 += text15;
			text15 = text15 << 05 | text15 >> (64-05);
			text15 ^= text00;
			// Mix text02 text11 20
			text02 += text11;
			text11 = text11 << 20 | text11 >> (64-20);
			text11 ^= text02;
			// Mix text06 text13 48
			text06 += text13;
			text13 = text13 << 48 | text13 >> (64-48);
			text13 ^= text06;
			// Mix text04 text09 41
			text04 += text09;
			text09 = text09 << 41 | text09 >> (64-41);
			text09 ^= text04;
			// Mix text14 text01 47
			text14 += text01;
			text01 = text01 << 47 | text01 >> (64-47);
			text01 ^= text14;
			// Mix text08 text05 28
			text08 += text05;
			text05 = text05 << 28 | text05 >> (64-28);
			text05 ^= text08;
			// Mix text10 text03 16
			text10 += text03;
			text03 = text03 << 16 | text03 >> (64-16);
			text03 ^= text10;
			// Mix text12 text07 25
			text12 += text07;
			text07 = text07 << 25 | text07 >> (64-25);
			text07 ^= text12;
			
			// round 12
			// Mix text00 text01 41
			text01 += key04;
			text00 += text01 + key03;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text00;
			// Mix text02 text03 09
			text03 += key06;
			text02 += text03 + key05;
			text03 = text03 << 09 | text03 >> (64-09);
			text03 ^= text02;
			// Mix text04 text05 37
			text05 += key08;
			text04 += text05 + key07;
			text05 = text05 << 37 | text05 >> (64-37);
			text05 ^= text04;
			// Mix text06 text07 31
			text07 += key10;
			text06 += text07 + key09;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text06;
			// Mix text08 text09 12
			text09 += key12;
			text08 += text09 + key11;
			text09 = text09 << 12 | text09 >> (64-12);
			text09 ^= text08;
			// Mix text10 text11 47
			text11 += key14;
			text10 += text11 + key13;
			text11 = text11 << 47 | text11 >> (64-47);
			text11 ^= text10;
			// Mix text12 text13 44
			text13 += key16;
			text12 += text13 + key15;
			text13 = text13 << 44 | text13 >> (64-44);
			text13 ^= text12;
			// Mix text14 text15 30
			text15 += key01 + 03;
			text14 += text15 + key00 + tweak01;
			text15 = text15 << 30 | text15 >> (64-30);
			text15 ^= text14;
			
			// round 13
			// Mix text00 text09 16
			text00 += text09;
			text09 = text09 << 16 | text09 >> (64-16);
			text09 ^= text00;
			// Mix text02 text13 34
			text02 += text13;
			text13 = text13 << 34 | text13 >> (64-34);
			text13 ^= text02;
			// Mix text06 text11 56
			text06 += text11;
			text11 = text11 << 56 | text11 >> (64-56);
			text11 ^= text06;
			// Mix text04 text15 51
			text04 += text15;
			text15 = text15 << 51 | text15 >> (64-51);
			text15 ^= text04;
			// Mix text10 text07 04
			text10 += text07;
			text07 = text07 << 04 | text07 >> (64-04);
			text07 ^= text10;
			// Mix text12 text03 53
			text12 += text03;
			text03 = text03 << 53 | text03 >> (64-53);
			text03 ^= text12;
			// Mix text14 text05 42
			text14 += text05;
			text05 = text05 << 42 | text05 >> (64-42);
			text05 ^= text14;
			// Mix text08 text01 41
			text08 += text01;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text08;
			
			// round 14
			// Mix text00 text07 31
			text00 += text07;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text00;
			// Mix text02 text05 44
			text02 += text05;
			text05 = text05 << 44 | text05 >> (64-44);
			text05 ^= text02;
			// Mix text04 text03 47
			text04 += text03;
			text03 = text03 << 47 | text03 >> (64-47);
			text03 ^= text04;
			// Mix text06 text01 46
			text06 += text01;
			text01 = text01 << 46 | text01 >> (64-46);
			text01 ^= text06;
			// Mix text12 text15 19
			text12 += text15;
			text15 = text15 << 19 | text15 >> (64-19);
			text15 ^= text12;
			// Mix text14 text13 42
			text14 += text13;
			text13 = text13 << 42 | text13 >> (64-42);
			text13 ^= text14;
			// Mix text08 text11 44
			text08 += text11;
			text11 = text11 << 44 | text11 >> (64-44);
			text11 ^= text08;
			// Mix text10 text09 25
			text10 += text09;
			text09 = text09 << 25 | text09 >> (64-25);
			text09 ^= text10;
			
			// round 15
			// Mix text00 text15 09
			text00 += text15;
			text15 = text15 << 09 | text15 >> (64-09);
			text15 ^= text00;
			// Mix text02 text11 48
			text02 += text11;
			text11 = text11 << 48 | text11 >> (64-48);
			text11 ^= text02;
			// Mix text06 text13 35
			text06 += text13;
			text13 = text13 << 35 | text13 >> (64-35);
			text13 ^= text06;
			// Mix text04 text09 52
			text04 += text09;
			text09 = text09 << 52 | text09 >> (64-52);
			text09 ^= text04;
			// Mix text14 text01 23
			text14 += text01;
			text01 = text01 << 23 | text01 >> (64-23);
			text01 ^= text14;
			// Mix text08 text05 31
			text08 += text05;
			text05 = text05 << 31 | text05 >> (64-31);
			text05 ^= text08;
			// Mix text10 text03 37
			text10 += text03;
			text03 = text03 << 37 | text03 >> (64-37);
			text03 ^= text10;
			// Mix text12 text07 20
			text12 += text07;
			text07 = text07 << 20 | text07 >> (64-20);
			text07 ^= text12;
			
			// round 16
			// Mix text00 text01 24
			text01 += key05;
			text00 += text01 + key04;
			text01 = text01 << 24 | text01 >> (64-24);
			text01 ^= text00;
			// Mix text02 text03 13
			text03 += key07;
			text02 += text03 + key06;
			text03 = text03 << 13 | text03 >> (64-13);
			text03 ^= text02;
			// Mix text04 text05 08
			text05 += key09;
			text04 += text05 + key08;
			text05 = text05 << 08 | text05 >> (64-08);
			text05 ^= text04;
			// Mix text06 text07 47
			text07 += key11;
			text06 += text07 + key10;
			text07 = text07 << 47 | text07 >> (64-47);
			text07 ^= text06;
			// Mix text08 text09 08
			text09 += key13;
			text08 += text09 + key12;
			text09 = text09 << 08 | text09 >> (64-08);
			text09 ^= text08;
			// Mix text10 text11 17
			text11 += key15;
			text10 += text11 + key14;
			text11 = text11 << 17 | text11 >> (64-17);
			text11 ^= text10;
			// Mix text12 text13 22
			text13 += key00;
			text12 += text13 + key16;
			text13 = text13 << 22 | text13 >> (64-22);
			text13 ^= text12;
			// Mix text14 text15 37
			text15 += key02 + 04;
			text14 += text15 + key01 + tweak02;
			text15 = text15 << 37 | text15 >> (64-37);
			text15 ^= text14;
			
			// round 17
			// Mix text00 text09 38
			text00 += text09;
			text09 = text09 << 38 | text09 >> (64-38);
			text09 ^= text00;
			// Mix text02 text13 19
			text02 += text13;
			text13 = text13 << 19 | text13 >> (64-19);
			text13 ^= text02;
			// Mix text06 text11 10
			text06 += text11;
			text11 = text11 << 10 | text11 >> (64-10);
			text11 ^= text06;
			// Mix text04 text15 55
			text04 += text15;
			text15 = text15 << 55 | text15 >> (64-55);
			text15 ^= text04;
			// Mix text10 text07 49
			text10 += text07;
			text07 = text07 << 49 | text07 >> (64-49);
			text07 ^= text10;
			// Mix text12 text03 18
			text12 += text03;
			text03 = text03 << 18 | text03 >> (64-18);
			text03 ^= text12;
			// Mix text14 text05 23
			text14 += text05;
			text05 = text05 << 23 | text05 >> (64-23);
			text05 ^= text14;
			// Mix text08 text01 52
			text08 += text01;
			text01 = text01 << 52 | text01 >> (64-52);
			text01 ^= text08;
			
			// round 18
			// Mix text00 text07 33
			text00 += text07;
			text07 = text07 << 33 | text07 >> (64-33);
			text07 ^= text00;
			// Mix text02 text05 04
			text02 += text05;
			text05 = text05 << 04 | text05 >> (64-04);
			text05 ^= text02;
			// Mix text04 text03 51
			text04 += text03;
			text03 = text03 << 51 | text03 >> (64-51);
			text03 ^= text04;
			// Mix text06 text01 13
			text06 += text01;
			text01 = text01 << 13 | text01 >> (64-13);
			text01 ^= text06;
			// Mix text12 text15 34
			text12 += text15;
			text15 = text15 << 34 | text15 >> (64-34);
			text15 ^= text12;
			// Mix text14 text13 41
			text14 += text13;
			text13 = text13 << 41 | text13 >> (64-41);
			text13 ^= text14;
			// Mix text08 text11 59
			text08 += text11;
			text11 = text11 << 59 | text11 >> (64-59);
			text11 ^= text08;
			// Mix text10 text09 17
			text10 += text09;
			text09 = text09 << 17 | text09 >> (64-17);
			text09 ^= text10;
			
			// round 19
			// Mix text00 text15 05
			text00 += text15;
			text15 = text15 << 05 | text15 >> (64-05);
			text15 ^= text00;
			// Mix text02 text11 20
			text02 += text11;
			text11 = text11 << 20 | text11 >> (64-20);
			text11 ^= text02;
			// Mix text06 text13 48
			text06 += text13;
			text13 = text13 << 48 | text13 >> (64-48);
			text13 ^= text06;
			// Mix text04 text09 41
			text04 += text09;
			text09 = text09 << 41 | text09 >> (64-41);
			text09 ^= text04;
			// Mix text14 text01 47
			text14 += text01;
			text01 = text01 << 47 | text01 >> (64-47);
			text01 ^= text14;
			// Mix text08 text05 28
			text08 += text05;
			text05 = text05 << 28 | text05 >> (64-28);
			text05 ^= text08;
			// Mix text10 text03 16
			text10 += text03;
			text03 = text03 << 16 | text03 >> (64-16);
			text03 ^= text10;
			// Mix text12 text07 25
			text12 += text07;
			text07 = text07 << 25 | text07 >> (64-25);
			text07 ^= text12;
			
			// round 20
			// Mix text00 text01 41
			text01 += key06;
			text00 += text01 + key05;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text00;
			// Mix text02 text03 09
			text03 += key08;
			text02 += text03 + key07;
			text03 = text03 << 09 | text03 >> (64-09);
			text03 ^= text02;
			// Mix text04 text05 37
			text05 += key10;
			text04 += text05 + key09;
			text05 = text05 << 37 | text05 >> (64-37);
			text05 ^= text04;
			// Mix text06 text07 31
			text07 += key12;
			text06 += text07 + key11;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text06;
			// Mix text08 text09 12
			text09 += key14;
			text08 += text09 + key13;
			text09 = text09 << 12 | text09 >> (64-12);
			text09 ^= text08;
			// Mix text10 text11 47
			text11 += key16;
			text10 += text11 + key15;
			text11 = text11 << 47 | text11 >> (64-47);
			text11 ^= text10;
			// Mix text12 text13 44
			text13 += key01;
			text12 += text13 + key00;
			text13 = text13 << 44 | text13 >> (64-44);
			text13 ^= text12;
			// Mix text14 text15 30
			text15 += key03 + 05;
			text14 += text15 + key02 + tweak00;
			text15 = text15 << 30 | text15 >> (64-30);
			text15 ^= text14;
			
			// round 21
			// Mix text00 text09 16
			text00 += text09;
			text09 = text09 << 16 | text09 >> (64-16);
			text09 ^= text00;
			// Mix text02 text13 34
			text02 += text13;
			text13 = text13 << 34 | text13 >> (64-34);
			text13 ^= text02;
			// Mix text06 text11 56
			text06 += text11;
			text11 = text11 << 56 | text11 >> (64-56);
			text11 ^= text06;
			// Mix text04 text15 51
			text04 += text15;
			text15 = text15 << 51 | text15 >> (64-51);
			text15 ^= text04;
			// Mix text10 text07 04
			text10 += text07;
			text07 = text07 << 04 | text07 >> (64-04);
			text07 ^= text10;
			// Mix text12 text03 53
			text12 += text03;
			text03 = text03 << 53 | text03 >> (64-53);
			text03 ^= text12;
			// Mix text14 text05 42
			text14 += text05;
			text05 = text05 << 42 | text05 >> (64-42);
			text05 ^= text14;
			// Mix text08 text01 41
			text08 += text01;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text08;
			
			// round 22
			// Mix text00 text07 31
			text00 += text07;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text00;
			// Mix text02 text05 44
			text02 += text05;
			text05 = text05 << 44 | text05 >> (64-44);
			text05 ^= text02;
			// Mix text04 text03 47
			text04 += text03;
			text03 = text03 << 47 | text03 >> (64-47);
			text03 ^= text04;
			// Mix text06 text01 46
			text06 += text01;
			text01 = text01 << 46 | text01 >> (64-46);
			text01 ^= text06;
			// Mix text12 text15 19
			text12 += text15;
			text15 = text15 << 19 | text15 >> (64-19);
			text15 ^= text12;
			// Mix text14 text13 42
			text14 += text13;
			text13 = text13 << 42 | text13 >> (64-42);
			text13 ^= text14;
			// Mix text08 text11 44
			text08 += text11;
			text11 = text11 << 44 | text11 >> (64-44);
			text11 ^= text08;
			// Mix text10 text09 25
			text10 += text09;
			text09 = text09 << 25 | text09 >> (64-25);
			text09 ^= text10;
			
			// round 23
			// Mix text00 text15 09
			text00 += text15;
			text15 = text15 << 09 | text15 >> (64-09);
			text15 ^= text00;
			// Mix text02 text11 48
			text02 += text11;
			text11 = text11 << 48 | text11 >> (64-48);
			text11 ^= text02;
			// Mix text06 text13 35
			text06 += text13;
			text13 = text13 << 35 | text13 >> (64-35);
			text13 ^= text06;
			// Mix text04 text09 52
			text04 += text09;
			text09 = text09 << 52 | text09 >> (64-52);
			text09 ^= text04;
			// Mix text14 text01 23
			text14 += text01;
			text01 = text01 << 23 | text01 >> (64-23);
			text01 ^= text14;
			// Mix text08 text05 31
			text08 += text05;
			text05 = text05 << 31 | text05 >> (64-31);
			text05 ^= text08;
			// Mix text10 text03 37
			text10 += text03;
			text03 = text03 << 37 | text03 >> (64-37);
			text03 ^= text10;
			// Mix text12 text07 20
			text12 += text07;
			text07 = text07 << 20 | text07 >> (64-20);
			text07 ^= text12;
			
			// round 24
			// Mix text00 text01 24
			text01 += key07;
			text00 += text01 + key06;
			text01 = text01 << 24 | text01 >> (64-24);
			text01 ^= text00;
			// Mix text02 text03 13
			text03 += key09;
			text02 += text03 + key08;
			text03 = text03 << 13 | text03 >> (64-13);
			text03 ^= text02;
			// Mix text04 text05 08
			text05 += key11;
			text04 += text05 + key10;
			text05 = text05 << 08 | text05 >> (64-08);
			text05 ^= text04;
			// Mix text06 text07 47
			text07 += key13;
			text06 += text07 + key12;
			text07 = text07 << 47 | text07 >> (64-47);
			text07 ^= text06;
			// Mix text08 text09 08
			text09 += key15;
			text08 += text09 + key14;
			text09 = text09 << 08 | text09 >> (64-08);
			text09 ^= text08;
			// Mix text10 text11 17
			text11 += key00;
			text10 += text11 + key16;
			text11 = text11 << 17 | text11 >> (64-17);
			text11 ^= text10;
			// Mix text12 text13 22
			text13 += key02;
			text12 += text13 + key01;
			text13 = text13 << 22 | text13 >> (64-22);
			text13 ^= text12;
			// Mix text14 text15 37
			text15 += key04 + 06;
			text14 += text15 + key03 + tweak01;
			text15 = text15 << 37 | text15 >> (64-37);
			text15 ^= text14;
			
			// round 25
			// Mix text00 text09 38
			text00 += text09;
			text09 = text09 << 38 | text09 >> (64-38);
			text09 ^= text00;
			// Mix text02 text13 19
			text02 += text13;
			text13 = text13 << 19 | text13 >> (64-19);
			text13 ^= text02;
			// Mix text06 text11 10
			text06 += text11;
			text11 = text11 << 10 | text11 >> (64-10);
			text11 ^= text06;
			// Mix text04 text15 55
			text04 += text15;
			text15 = text15 << 55 | text15 >> (64-55);
			text15 ^= text04;
			// Mix text10 text07 49
			text10 += text07;
			text07 = text07 << 49 | text07 >> (64-49);
			text07 ^= text10;
			// Mix text12 text03 18
			text12 += text03;
			text03 = text03 << 18 | text03 >> (64-18);
			text03 ^= text12;
			// Mix text14 text05 23
			text14 += text05;
			text05 = text05 << 23 | text05 >> (64-23);
			text05 ^= text14;
			// Mix text08 text01 52
			text08 += text01;
			text01 = text01 << 52 | text01 >> (64-52);
			text01 ^= text08;
			
			// round 26
			// Mix text00 text07 33
			text00 += text07;
			text07 = text07 << 33 | text07 >> (64-33);
			text07 ^= text00;
			// Mix text02 text05 04
			text02 += text05;
			text05 = text05 << 04 | text05 >> (64-04);
			text05 ^= text02;
			// Mix text04 text03 51
			text04 += text03;
			text03 = text03 << 51 | text03 >> (64-51);
			text03 ^= text04;
			// Mix text06 text01 13
			text06 += text01;
			text01 = text01 << 13 | text01 >> (64-13);
			text01 ^= text06;
			// Mix text12 text15 34
			text12 += text15;
			text15 = text15 << 34 | text15 >> (64-34);
			text15 ^= text12;
			// Mix text14 text13 41
			text14 += text13;
			text13 = text13 << 41 | text13 >> (64-41);
			text13 ^= text14;
			// Mix text08 text11 59
			text08 += text11;
			text11 = text11 << 59 | text11 >> (64-59);
			text11 ^= text08;
			// Mix text10 text09 17
			text10 += text09;
			text09 = text09 << 17 | text09 >> (64-17);
			text09 ^= text10;
			
			// round 27
			// Mix text00 text15 05
			text00 += text15;
			text15 = text15 << 05 | text15 >> (64-05);
			text15 ^= text00;
			// Mix text02 text11 20
			text02 += text11;
			text11 = text11 << 20 | text11 >> (64-20);
			text11 ^= text02;
			// Mix text06 text13 48
			text06 += text13;
			text13 = text13 << 48 | text13 >> (64-48);
			text13 ^= text06;
			// Mix text04 text09 41
			text04 += text09;
			text09 = text09 << 41 | text09 >> (64-41);
			text09 ^= text04;
			// Mix text14 text01 47
			text14 += text01;
			text01 = text01 << 47 | text01 >> (64-47);
			text01 ^= text14;
			// Mix text08 text05 28
			text08 += text05;
			text05 = text05 << 28 | text05 >> (64-28);
			text05 ^= text08;
			// Mix text10 text03 16
			text10 += text03;
			text03 = text03 << 16 | text03 >> (64-16);
			text03 ^= text10;
			// Mix text12 text07 25
			text12 += text07;
			text07 = text07 << 25 | text07 >> (64-25);
			text07 ^= text12;
			
			// round 28
			// Mix text00 text01 41
			text01 += key08;
			text00 += text01 + key07;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text00;
			// Mix text02 text03 09
			text03 += key10;
			text02 += text03 + key09;
			text03 = text03 << 09 | text03 >> (64-09);
			text03 ^= text02;
			// Mix text04 text05 37
			text05 += key12;
			text04 += text05 + key11;
			text05 = text05 << 37 | text05 >> (64-37);
			text05 ^= text04;
			// Mix text06 text07 31
			text07 += key14;
			text06 += text07 + key13;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text06;
			// Mix text08 text09 12
			text09 += key16;
			text08 += text09 + key15;
			text09 = text09 << 12 | text09 >> (64-12);
			text09 ^= text08;
			// Mix text10 text11 47
			text11 += key01;
			text10 += text11 + key00;
			text11 = text11 << 47 | text11 >> (64-47);
			text11 ^= text10;
			// Mix text12 text13 44
			text13 += key03;
			text12 += text13 + key02;
			text13 = text13 << 44 | text13 >> (64-44);
			text13 ^= text12;
			// Mix text14 text15 30
			text15 += key05 + 07;
			text14 += text15 + key04 + tweak02;
			text15 = text15 << 30 | text15 >> (64-30);
			text15 ^= text14;
			
			// round 29
			// Mix text00 text09 16
			text00 += text09;
			text09 = text09 << 16 | text09 >> (64-16);
			text09 ^= text00;
			// Mix text02 text13 34
			text02 += text13;
			text13 = text13 << 34 | text13 >> (64-34);
			text13 ^= text02;
			// Mix text06 text11 56
			text06 += text11;
			text11 = text11 << 56 | text11 >> (64-56);
			text11 ^= text06;
			// Mix text04 text15 51
			text04 += text15;
			text15 = text15 << 51 | text15 >> (64-51);
			text15 ^= text04;
			// Mix text10 text07 04
			text10 += text07;
			text07 = text07 << 04 | text07 >> (64-04);
			text07 ^= text10;
			// Mix text12 text03 53
			text12 += text03;
			text03 = text03 << 53 | text03 >> (64-53);
			text03 ^= text12;
			// Mix text14 text05 42
			text14 += text05;
			text05 = text05 << 42 | text05 >> (64-42);
			text05 ^= text14;
			// Mix text08 text01 41
			text08 += text01;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text08;
			
			// round 30
			// Mix text00 text07 31
			text00 += text07;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text00;
			// Mix text02 text05 44
			text02 += text05;
			text05 = text05 << 44 | text05 >> (64-44);
			text05 ^= text02;
			// Mix text04 text03 47
			text04 += text03;
			text03 = text03 << 47 | text03 >> (64-47);
			text03 ^= text04;
			// Mix text06 text01 46
			text06 += text01;
			text01 = text01 << 46 | text01 >> (64-46);
			text01 ^= text06;
			// Mix text12 text15 19
			text12 += text15;
			text15 = text15 << 19 | text15 >> (64-19);
			text15 ^= text12;
			// Mix text14 text13 42
			text14 += text13;
			text13 = text13 << 42 | text13 >> (64-42);
			text13 ^= text14;
			// Mix text08 text11 44
			text08 += text11;
			text11 = text11 << 44 | text11 >> (64-44);
			text11 ^= text08;
			// Mix text10 text09 25
			text10 += text09;
			text09 = text09 << 25 | text09 >> (64-25);
			text09 ^= text10;
			
			// round 31
			// Mix text00 text15 09
			text00 += text15;
			text15 = text15 << 09 | text15 >> (64-09);
			text15 ^= text00;
			// Mix text02 text11 48
			text02 += text11;
			text11 = text11 << 48 | text11 >> (64-48);
			text11 ^= text02;
			// Mix text06 text13 35
			text06 += text13;
			text13 = text13 << 35 | text13 >> (64-35);
			text13 ^= text06;
			// Mix text04 text09 52
			text04 += text09;
			text09 = text09 << 52 | text09 >> (64-52);
			text09 ^= text04;
			// Mix text14 text01 23
			text14 += text01;
			text01 = text01 << 23 | text01 >> (64-23);
			text01 ^= text14;
			// Mix text08 text05 31
			text08 += text05;
			text05 = text05 << 31 | text05 >> (64-31);
			text05 ^= text08;
			// Mix text10 text03 37
			text10 += text03;
			text03 = text03 << 37 | text03 >> (64-37);
			text03 ^= text10;
			// Mix text12 text07 20
			text12 += text07;
			text07 = text07 << 20 | text07 >> (64-20);
			text07 ^= text12;
			
			// round 32
			// Mix text00 text01 24
			text01 += key09;
			text00 += text01 + key08;
			text01 = text01 << 24 | text01 >> (64-24);
			text01 ^= text00;
			// Mix text02 text03 13
			text03 += key11;
			text02 += text03 + key10;
			text03 = text03 << 13 | text03 >> (64-13);
			text03 ^= text02;
			// Mix text04 text05 08
			text05 += key13;
			text04 += text05 + key12;
			text05 = text05 << 08 | text05 >> (64-08);
			text05 ^= text04;
			// Mix text06 text07 47
			text07 += key15;
			text06 += text07 + key14;
			text07 = text07 << 47 | text07 >> (64-47);
			text07 ^= text06;
			// Mix text08 text09 08
			text09 += key00;
			text08 += text09 + key16;
			text09 = text09 << 08 | text09 >> (64-08);
			text09 ^= text08;
			// Mix text10 text11 17
			text11 += key02;
			text10 += text11 + key01;
			text11 = text11 << 17 | text11 >> (64-17);
			text11 ^= text10;
			// Mix text12 text13 22
			text13 += key04;
			text12 += text13 + key03;
			text13 = text13 << 22 | text13 >> (64-22);
			text13 ^= text12;
			// Mix text14 text15 37
			text15 += key06 + 08;
			text14 += text15 + key05 + tweak00;
			text15 = text15 << 37 | text15 >> (64-37);
			text15 ^= text14;
			
			// round 33
			// Mix text00 text09 38
			text00 += text09;
			text09 = text09 << 38 | text09 >> (64-38);
			text09 ^= text00;
			// Mix text02 text13 19
			text02 += text13;
			text13 = text13 << 19 | text13 >> (64-19);
			text13 ^= text02;
			// Mix text06 text11 10
			text06 += text11;
			text11 = text11 << 10 | text11 >> (64-10);
			text11 ^= text06;
			// Mix text04 text15 55
			text04 += text15;
			text15 = text15 << 55 | text15 >> (64-55);
			text15 ^= text04;
			// Mix text10 text07 49
			text10 += text07;
			text07 = text07 << 49 | text07 >> (64-49);
			text07 ^= text10;
			// Mix text12 text03 18
			text12 += text03;
			text03 = text03 << 18 | text03 >> (64-18);
			text03 ^= text12;
			// Mix text14 text05 23
			text14 += text05;
			text05 = text05 << 23 | text05 >> (64-23);
			text05 ^= text14;
			// Mix text08 text01 52
			text08 += text01;
			text01 = text01 << 52 | text01 >> (64-52);
			text01 ^= text08;
			
			// round 34
			// Mix text00 text07 33
			text00 += text07;
			text07 = text07 << 33 | text07 >> (64-33);
			text07 ^= text00;
			// Mix text02 text05 04
			text02 += text05;
			text05 = text05 << 04 | text05 >> (64-04);
			text05 ^= text02;
			// Mix text04 text03 51
			text04 += text03;
			text03 = text03 << 51 | text03 >> (64-51);
			text03 ^= text04;
			// Mix text06 text01 13
			text06 += text01;
			text01 = text01 << 13 | text01 >> (64-13);
			text01 ^= text06;
			// Mix text12 text15 34
			text12 += text15;
			text15 = text15 << 34 | text15 >> (64-34);
			text15 ^= text12;
			// Mix text14 text13 41
			text14 += text13;
			text13 = text13 << 41 | text13 >> (64-41);
			text13 ^= text14;
			// Mix text08 text11 59
			text08 += text11;
			text11 = text11 << 59 | text11 >> (64-59);
			text11 ^= text08;
			// Mix text10 text09 17
			text10 += text09;
			text09 = text09 << 17 | text09 >> (64-17);
			text09 ^= text10;
			
			// round 35
			// Mix text00 text15 05
			text00 += text15;
			text15 = text15 << 05 | text15 >> (64-05);
			text15 ^= text00;
			// Mix text02 text11 20
			text02 += text11;
			text11 = text11 << 20 | text11 >> (64-20);
			text11 ^= text02;
			// Mix text06 text13 48
			text06 += text13;
			text13 = text13 << 48 | text13 >> (64-48);
			text13 ^= text06;
			// Mix text04 text09 41
			text04 += text09;
			text09 = text09 << 41 | text09 >> (64-41);
			text09 ^= text04;
			// Mix text14 text01 47
			text14 += text01;
			text01 = text01 << 47 | text01 >> (64-47);
			text01 ^= text14;
			// Mix text08 text05 28
			text08 += text05;
			text05 = text05 << 28 | text05 >> (64-28);
			text05 ^= text08;
			// Mix text10 text03 16
			text10 += text03;
			text03 = text03 << 16 | text03 >> (64-16);
			text03 ^= text10;
			// Mix text12 text07 25
			text12 += text07;
			text07 = text07 << 25 | text07 >> (64-25);
			text07 ^= text12;
			
			// round 36
			// Mix text00 text01 41
			text01 += key10;
			text00 += text01 + key09;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text00;
			// Mix text02 text03 09
			text03 += key12;
			text02 += text03 + key11;
			text03 = text03 << 09 | text03 >> (64-09);
			text03 ^= text02;
			// Mix text04 text05 37
			text05 += key14;
			text04 += text05 + key13;
			text05 = text05 << 37 | text05 >> (64-37);
			text05 ^= text04;
			// Mix text06 text07 31
			text07 += key16;
			text06 += text07 + key15;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text06;
			// Mix text08 text09 12
			text09 += key01;
			text08 += text09 + key00;
			text09 = text09 << 12 | text09 >> (64-12);
			text09 ^= text08;
			// Mix text10 text11 47
			text11 += key03;
			text10 += text11 + key02;
			text11 = text11 << 47 | text11 >> (64-47);
			text11 ^= text10;
			// Mix text12 text13 44
			text13 += key05;
			text12 += text13 + key04;
			text13 = text13 << 44 | text13 >> (64-44);
			text13 ^= text12;
			// Mix text14 text15 30
			text15 += key07 + 09;
			text14 += text15 + key06 + tweak01;
			text15 = text15 << 30 | text15 >> (64-30);
			text15 ^= text14;
			
			// round 37
			// Mix text00 text09 16
			text00 += text09;
			text09 = text09 << 16 | text09 >> (64-16);
			text09 ^= text00;
			// Mix text02 text13 34
			text02 += text13;
			text13 = text13 << 34 | text13 >> (64-34);
			text13 ^= text02;
			// Mix text06 text11 56
			text06 += text11;
			text11 = text11 << 56 | text11 >> (64-56);
			text11 ^= text06;
			// Mix text04 text15 51
			text04 += text15;
			text15 = text15 << 51 | text15 >> (64-51);
			text15 ^= text04;
			// Mix text10 text07 04
			text10 += text07;
			text07 = text07 << 04 | text07 >> (64-04);
			text07 ^= text10;
			// Mix text12 text03 53
			text12 += text03;
			text03 = text03 << 53 | text03 >> (64-53);
			text03 ^= text12;
			// Mix text14 text05 42
			text14 += text05;
			text05 = text05 << 42 | text05 >> (64-42);
			text05 ^= text14;
			// Mix text08 text01 41
			text08 += text01;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text08;
			
			// round 38
			// Mix text00 text07 31
			text00 += text07;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text00;
			// Mix text02 text05 44
			text02 += text05;
			text05 = text05 << 44 | text05 >> (64-44);
			text05 ^= text02;
			// Mix text04 text03 47
			text04 += text03;
			text03 = text03 << 47 | text03 >> (64-47);
			text03 ^= text04;
			// Mix text06 text01 46
			text06 += text01;
			text01 = text01 << 46 | text01 >> (64-46);
			text01 ^= text06;
			// Mix text12 text15 19
			text12 += text15;
			text15 = text15 << 19 | text15 >> (64-19);
			text15 ^= text12;
			// Mix text14 text13 42
			text14 += text13;
			text13 = text13 << 42 | text13 >> (64-42);
			text13 ^= text14;
			// Mix text08 text11 44
			text08 += text11;
			text11 = text11 << 44 | text11 >> (64-44);
			text11 ^= text08;
			// Mix text10 text09 25
			text10 += text09;
			text09 = text09 << 25 | text09 >> (64-25);
			text09 ^= text10;
			
			// round 39
			// Mix text00 text15 09
			text00 += text15;
			text15 = text15 << 09 | text15 >> (64-09);
			text15 ^= text00;
			// Mix text02 text11 48
			text02 += text11;
			text11 = text11 << 48 | text11 >> (64-48);
			text11 ^= text02;
			// Mix text06 text13 35
			text06 += text13;
			text13 = text13 << 35 | text13 >> (64-35);
			text13 ^= text06;
			// Mix text04 text09 52
			text04 += text09;
			text09 = text09 << 52 | text09 >> (64-52);
			text09 ^= text04;
			// Mix text14 text01 23
			text14 += text01;
			text01 = text01 << 23 | text01 >> (64-23);
			text01 ^= text14;
			// Mix text08 text05 31
			text08 += text05;
			text05 = text05 << 31 | text05 >> (64-31);
			text05 ^= text08;
			// Mix text10 text03 37
			text10 += text03;
			text03 = text03 << 37 | text03 >> (64-37);
			text03 ^= text10;
			// Mix text12 text07 20
			text12 += text07;
			text07 = text07 << 20 | text07 >> (64-20);
			text07 ^= text12;
			
			// round 40
			// Mix text00 text01 24
			text01 += key11;
			text00 += text01 + key10;
			text01 = text01 << 24 | text01 >> (64-24);
			text01 ^= text00;
			// Mix text02 text03 13
			text03 += key13;
			text02 += text03 + key12;
			text03 = text03 << 13 | text03 >> (64-13);
			text03 ^= text02;
			// Mix text04 text05 08
			text05 += key15;
			text04 += text05 + key14;
			text05 = text05 << 08 | text05 >> (64-08);
			text05 ^= text04;
			// Mix text06 text07 47
			text07 += key00;
			text06 += text07 + key16;
			text07 = text07 << 47 | text07 >> (64-47);
			text07 ^= text06;
			// Mix text08 text09 08
			text09 += key02;
			text08 += text09 + key01;
			text09 = text09 << 08 | text09 >> (64-08);
			text09 ^= text08;
			// Mix text10 text11 17
			text11 += key04;
			text10 += text11 + key03;
			text11 = text11 << 17 | text11 >> (64-17);
			text11 ^= text10;
			// Mix text12 text13 22
			text13 += key06;
			text12 += text13 + key05;
			text13 = text13 << 22 | text13 >> (64-22);
			text13 ^= text12;
			// Mix text14 text15 37
			text15 += key08 + 10;
			text14 += text15 + key07 + tweak02;
			text15 = text15 << 37 | text15 >> (64-37);
			text15 ^= text14;
			
			// round 41
			// Mix text00 text09 38
			text00 += text09;
			text09 = text09 << 38 | text09 >> (64-38);
			text09 ^= text00;
			// Mix text02 text13 19
			text02 += text13;
			text13 = text13 << 19 | text13 >> (64-19);
			text13 ^= text02;
			// Mix text06 text11 10
			text06 += text11;
			text11 = text11 << 10 | text11 >> (64-10);
			text11 ^= text06;
			// Mix text04 text15 55
			text04 += text15;
			text15 = text15 << 55 | text15 >> (64-55);
			text15 ^= text04;
			// Mix text10 text07 49
			text10 += text07;
			text07 = text07 << 49 | text07 >> (64-49);
			text07 ^= text10;
			// Mix text12 text03 18
			text12 += text03;
			text03 = text03 << 18 | text03 >> (64-18);
			text03 ^= text12;
			// Mix text14 text05 23
			text14 += text05;
			text05 = text05 << 23 | text05 >> (64-23);
			text05 ^= text14;
			// Mix text08 text01 52
			text08 += text01;
			text01 = text01 << 52 | text01 >> (64-52);
			text01 ^= text08;
			
			// round 42
			// Mix text00 text07 33
			text00 += text07;
			text07 = text07 << 33 | text07 >> (64-33);
			text07 ^= text00;
			// Mix text02 text05 04
			text02 += text05;
			text05 = text05 << 04 | text05 >> (64-04);
			text05 ^= text02;
			// Mix text04 text03 51
			text04 += text03;
			text03 = text03 << 51 | text03 >> (64-51);
			text03 ^= text04;
			// Mix text06 text01 13
			text06 += text01;
			text01 = text01 << 13 | text01 >> (64-13);
			text01 ^= text06;
			// Mix text12 text15 34
			text12 += text15;
			text15 = text15 << 34 | text15 >> (64-34);
			text15 ^= text12;
			// Mix text14 text13 41
			text14 += text13;
			text13 = text13 << 41 | text13 >> (64-41);
			text13 ^= text14;
			// Mix text08 text11 59
			text08 += text11;
			text11 = text11 << 59 | text11 >> (64-59);
			text11 ^= text08;
			// Mix text10 text09 17
			text10 += text09;
			text09 = text09 << 17 | text09 >> (64-17);
			text09 ^= text10;
			
			// round 43
			// Mix text00 text15 05
			text00 += text15;
			text15 = text15 << 05 | text15 >> (64-05);
			text15 ^= text00;
			// Mix text02 text11 20
			text02 += text11;
			text11 = text11 << 20 | text11 >> (64-20);
			text11 ^= text02;
			// Mix text06 text13 48
			text06 += text13;
			text13 = text13 << 48 | text13 >> (64-48);
			text13 ^= text06;
			// Mix text04 text09 41
			text04 += text09;
			text09 = text09 << 41 | text09 >> (64-41);
			text09 ^= text04;
			// Mix text14 text01 47
			text14 += text01;
			text01 = text01 << 47 | text01 >> (64-47);
			text01 ^= text14;
			// Mix text08 text05 28
			text08 += text05;
			text05 = text05 << 28 | text05 >> (64-28);
			text05 ^= text08;
			// Mix text10 text03 16
			text10 += text03;
			text03 = text03 << 16 | text03 >> (64-16);
			text03 ^= text10;
			// Mix text12 text07 25
			text12 += text07;
			text07 = text07 << 25 | text07 >> (64-25);
			text07 ^= text12;
			
			// round 44
			// Mix text00 text01 41
			text01 += key12;
			text00 += text01 + key11;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text00;
			// Mix text02 text03 09
			text03 += key14;
			text02 += text03 + key13;
			text03 = text03 << 09 | text03 >> (64-09);
			text03 ^= text02;
			// Mix text04 text05 37
			text05 += key16;
			text04 += text05 + key15;
			text05 = text05 << 37 | text05 >> (64-37);
			text05 ^= text04;
			// Mix text06 text07 31
			text07 += key01;
			text06 += text07 + key00;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text06;
			// Mix text08 text09 12
			text09 += key03;
			text08 += text09 + key02;
			text09 = text09 << 12 | text09 >> (64-12);
			text09 ^= text08;
			// Mix text10 text11 47
			text11 += key05;
			text10 += text11 + key04;
			text11 = text11 << 47 | text11 >> (64-47);
			text11 ^= text10;
			// Mix text12 text13 44
			text13 += key07;
			text12 += text13 + key06;
			text13 = text13 << 44 | text13 >> (64-44);
			text13 ^= text12;
			// Mix text14 text15 30
			text15 += key09 + 11;
			text14 += text15 + key08 + tweak00;
			text15 = text15 << 30 | text15 >> (64-30);
			text15 ^= text14;
			
			// round 45
			// Mix text00 text09 16
			text00 += text09;
			text09 = text09 << 16 | text09 >> (64-16);
			text09 ^= text00;
			// Mix text02 text13 34
			text02 += text13;
			text13 = text13 << 34 | text13 >> (64-34);
			text13 ^= text02;
			// Mix text06 text11 56
			text06 += text11;
			text11 = text11 << 56 | text11 >> (64-56);
			text11 ^= text06;
			// Mix text04 text15 51
			text04 += text15;
			text15 = text15 << 51 | text15 >> (64-51);
			text15 ^= text04;
			// Mix text10 text07 04
			text10 += text07;
			text07 = text07 << 04 | text07 >> (64-04);
			text07 ^= text10;
			// Mix text12 text03 53
			text12 += text03;
			text03 = text03 << 53 | text03 >> (64-53);
			text03 ^= text12;
			// Mix text14 text05 42
			text14 += text05;
			text05 = text05 << 42 | text05 >> (64-42);
			text05 ^= text14;
			// Mix text08 text01 41
			text08 += text01;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text08;
			
			// round 46
			// Mix text00 text07 31
			text00 += text07;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text00;
			// Mix text02 text05 44
			text02 += text05;
			text05 = text05 << 44 | text05 >> (64-44);
			text05 ^= text02;
			// Mix text04 text03 47
			text04 += text03;
			text03 = text03 << 47 | text03 >> (64-47);
			text03 ^= text04;
			// Mix text06 text01 46
			text06 += text01;
			text01 = text01 << 46 | text01 >> (64-46);
			text01 ^= text06;
			// Mix text12 text15 19
			text12 += text15;
			text15 = text15 << 19 | text15 >> (64-19);
			text15 ^= text12;
			// Mix text14 text13 42
			text14 += text13;
			text13 = text13 << 42 | text13 >> (64-42);
			text13 ^= text14;
			// Mix text08 text11 44
			text08 += text11;
			text11 = text11 << 44 | text11 >> (64-44);
			text11 ^= text08;
			// Mix text10 text09 25
			text10 += text09;
			text09 = text09 << 25 | text09 >> (64-25);
			text09 ^= text10;
			
			// round 47
			// Mix text00 text15 09
			text00 += text15;
			text15 = text15 << 09 | text15 >> (64-09);
			text15 ^= text00;
			// Mix text02 text11 48
			text02 += text11;
			text11 = text11 << 48 | text11 >> (64-48);
			text11 ^= text02;
			// Mix text06 text13 35
			text06 += text13;
			text13 = text13 << 35 | text13 >> (64-35);
			text13 ^= text06;
			// Mix text04 text09 52
			text04 += text09;
			text09 = text09 << 52 | text09 >> (64-52);
			text09 ^= text04;
			// Mix text14 text01 23
			text14 += text01;
			text01 = text01 << 23 | text01 >> (64-23);
			text01 ^= text14;
			// Mix text08 text05 31
			text08 += text05;
			text05 = text05 << 31 | text05 >> (64-31);
			text05 ^= text08;
			// Mix text10 text03 37
			text10 += text03;
			text03 = text03 << 37 | text03 >> (64-37);
			text03 ^= text10;
			// Mix text12 text07 20
			text12 += text07;
			text07 = text07 << 20 | text07 >> (64-20);
			text07 ^= text12;
			
			// round 48
			// Mix text00 text01 24
			text01 += key13;
			text00 += text01 + key12;
			text01 = text01 << 24 | text01 >> (64-24);
			text01 ^= text00;
			// Mix text02 text03 13
			text03 += key15;
			text02 += text03 + key14;
			text03 = text03 << 13 | text03 >> (64-13);
			text03 ^= text02;
			// Mix text04 text05 08
			text05 += key00;
			text04 += text05 + key16;
			text05 = text05 << 08 | text05 >> (64-08);
			text05 ^= text04;
			// Mix text06 text07 47
			text07 += key02;
			text06 += text07 + key01;
			text07 = text07 << 47 | text07 >> (64-47);
			text07 ^= text06;
			// Mix text08 text09 08
			text09 += key04;
			text08 += text09 + key03;
			text09 = text09 << 08 | text09 >> (64-08);
			text09 ^= text08;
			// Mix text10 text11 17
			text11 += key06;
			text10 += text11 + key05;
			text11 = text11 << 17 | text11 >> (64-17);
			text11 ^= text10;
			// Mix text12 text13 22
			text13 += key08;
			text12 += text13 + key07;
			text13 = text13 << 22 | text13 >> (64-22);
			text13 ^= text12;
			// Mix text14 text15 37
			text15 += key10 + 12;
			text14 += text15 + key09 + tweak01;
			text15 = text15 << 37 | text15 >> (64-37);
			text15 ^= text14;
			
			// round 49
			// Mix text00 text09 38
			text00 += text09;
			text09 = text09 << 38 | text09 >> (64-38);
			text09 ^= text00;
			// Mix text02 text13 19
			text02 += text13;
			text13 = text13 << 19 | text13 >> (64-19);
			text13 ^= text02;
			// Mix text06 text11 10
			text06 += text11;
			text11 = text11 << 10 | text11 >> (64-10);
			text11 ^= text06;
			// Mix text04 text15 55
			text04 += text15;
			text15 = text15 << 55 | text15 >> (64-55);
			text15 ^= text04;
			// Mix text10 text07 49
			text10 += text07;
			text07 = text07 << 49 | text07 >> (64-49);
			text07 ^= text10;
			// Mix text12 text03 18
			text12 += text03;
			text03 = text03 << 18 | text03 >> (64-18);
			text03 ^= text12;
			// Mix text14 text05 23
			text14 += text05;
			text05 = text05 << 23 | text05 >> (64-23);
			text05 ^= text14;
			// Mix text08 text01 52
			text08 += text01;
			text01 = text01 << 52 | text01 >> (64-52);
			text01 ^= text08;
			
			// round 50
			// Mix text00 text07 33
			text00 += text07;
			text07 = text07 << 33 | text07 >> (64-33);
			text07 ^= text00;
			// Mix text02 text05 04
			text02 += text05;
			text05 = text05 << 04 | text05 >> (64-04);
			text05 ^= text02;
			// Mix text04 text03 51
			text04 += text03;
			text03 = text03 << 51 | text03 >> (64-51);
			text03 ^= text04;
			// Mix text06 text01 13
			text06 += text01;
			text01 = text01 << 13 | text01 >> (64-13);
			text01 ^= text06;
			// Mix text12 text15 34
			text12 += text15;
			text15 = text15 << 34 | text15 >> (64-34);
			text15 ^= text12;
			// Mix text14 text13 41
			text14 += text13;
			text13 = text13 << 41 | text13 >> (64-41);
			text13 ^= text14;
			// Mix text08 text11 59
			text08 += text11;
			text11 = text11 << 59 | text11 >> (64-59);
			text11 ^= text08;
			// Mix text10 text09 17
			text10 += text09;
			text09 = text09 << 17 | text09 >> (64-17);
			text09 ^= text10;
			
			// round 51
			// Mix text00 text15 05
			text00 += text15;
			text15 = text15 << 05 | text15 >> (64-05);
			text15 ^= text00;
			// Mix text02 text11 20
			text02 += text11;
			text11 = text11 << 20 | text11 >> (64-20);
			text11 ^= text02;
			// Mix text06 text13 48
			text06 += text13;
			text13 = text13 << 48 | text13 >> (64-48);
			text13 ^= text06;
			// Mix text04 text09 41
			text04 += text09;
			text09 = text09 << 41 | text09 >> (64-41);
			text09 ^= text04;
			// Mix text14 text01 47
			text14 += text01;
			text01 = text01 << 47 | text01 >> (64-47);
			text01 ^= text14;
			// Mix text08 text05 28
			text08 += text05;
			text05 = text05 << 28 | text05 >> (64-28);
			text05 ^= text08;
			// Mix text10 text03 16
			text10 += text03;
			text03 = text03 << 16 | text03 >> (64-16);
			text03 ^= text10;
			// Mix text12 text07 25
			text12 += text07;
			text07 = text07 << 25 | text07 >> (64-25);
			text07 ^= text12;
			
			// round 52
			// Mix text00 text01 41
			text01 += key14;
			text00 += text01 + key13;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text00;
			// Mix text02 text03 09
			text03 += key16;
			text02 += text03 + key15;
			text03 = text03 << 09 | text03 >> (64-09);
			text03 ^= text02;
			// Mix text04 text05 37
			text05 += key01;
			text04 += text05 + key00;
			text05 = text05 << 37 | text05 >> (64-37);
			text05 ^= text04;
			// Mix text06 text07 31
			text07 += key03;
			text06 += text07 + key02;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text06;
			// Mix text08 text09 12
			text09 += key05;
			text08 += text09 + key04;
			text09 = text09 << 12 | text09 >> (64-12);
			text09 ^= text08;
			// Mix text10 text11 47
			text11 += key07;
			text10 += text11 + key06;
			text11 = text11 << 47 | text11 >> (64-47);
			text11 ^= text10;
			// Mix text12 text13 44
			text13 += key09;
			text12 += text13 + key08;
			text13 = text13 << 44 | text13 >> (64-44);
			text13 ^= text12;
			// Mix text14 text15 30
			text15 += key11 + 13;
			text14 += text15 + key10 + tweak02;
			text15 = text15 << 30 | text15 >> (64-30);
			text15 ^= text14;
			
			// round 53
			// Mix text00 text09 16
			text00 += text09;
			text09 = text09 << 16 | text09 >> (64-16);
			text09 ^= text00;
			// Mix text02 text13 34
			text02 += text13;
			text13 = text13 << 34 | text13 >> (64-34);
			text13 ^= text02;
			// Mix text06 text11 56
			text06 += text11;
			text11 = text11 << 56 | text11 >> (64-56);
			text11 ^= text06;
			// Mix text04 text15 51
			text04 += text15;
			text15 = text15 << 51 | text15 >> (64-51);
			text15 ^= text04;
			// Mix text10 text07 04
			text10 += text07;
			text07 = text07 << 04 | text07 >> (64-04);
			text07 ^= text10;
			// Mix text12 text03 53
			text12 += text03;
			text03 = text03 << 53 | text03 >> (64-53);
			text03 ^= text12;
			// Mix text14 text05 42
			text14 += text05;
			text05 = text05 << 42 | text05 >> (64-42);
			text05 ^= text14;
			// Mix text08 text01 41
			text08 += text01;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text08;
			
			// round 54
			// Mix text00 text07 31
			text00 += text07;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text00;
			// Mix text02 text05 44
			text02 += text05;
			text05 = text05 << 44 | text05 >> (64-44);
			text05 ^= text02;
			// Mix text04 text03 47
			text04 += text03;
			text03 = text03 << 47 | text03 >> (64-47);
			text03 ^= text04;
			// Mix text06 text01 46
			text06 += text01;
			text01 = text01 << 46 | text01 >> (64-46);
			text01 ^= text06;
			// Mix text12 text15 19
			text12 += text15;
			text15 = text15 << 19 | text15 >> (64-19);
			text15 ^= text12;
			// Mix text14 text13 42
			text14 += text13;
			text13 = text13 << 42 | text13 >> (64-42);
			text13 ^= text14;
			// Mix text08 text11 44
			text08 += text11;
			text11 = text11 << 44 | text11 >> (64-44);
			text11 ^= text08;
			// Mix text10 text09 25
			text10 += text09;
			text09 = text09 << 25 | text09 >> (64-25);
			text09 ^= text10;
			
			// round 55
			// Mix text00 text15 09
			text00 += text15;
			text15 = text15 << 09 | text15 >> (64-09);
			text15 ^= text00;
			// Mix text02 text11 48
			text02 += text11;
			text11 = text11 << 48 | text11 >> (64-48);
			text11 ^= text02;
			// Mix text06 text13 35
			text06 += text13;
			text13 = text13 << 35 | text13 >> (64-35);
			text13 ^= text06;
			// Mix text04 text09 52
			text04 += text09;
			text09 = text09 << 52 | text09 >> (64-52);
			text09 ^= text04;
			// Mix text14 text01 23
			text14 += text01;
			text01 = text01 << 23 | text01 >> (64-23);
			text01 ^= text14;
			// Mix text08 text05 31
			text08 += text05;
			text05 = text05 << 31 | text05 >> (64-31);
			text05 ^= text08;
			// Mix text10 text03 37
			text10 += text03;
			text03 = text03 << 37 | text03 >> (64-37);
			text03 ^= text10;
			// Mix text12 text07 20
			text12 += text07;
			text07 = text07 << 20 | text07 >> (64-20);
			text07 ^= text12;
			
			// round 56
			// Mix text00 text01 24
			text01 += key15;
			text00 += text01 + key14;
			text01 = text01 << 24 | text01 >> (64-24);
			text01 ^= text00;
			// Mix text02 text03 13
			text03 += key00;
			text02 += text03 + key16;
			text03 = text03 << 13 | text03 >> (64-13);
			text03 ^= text02;
			// Mix text04 text05 08
			text05 += key02;
			text04 += text05 + key01;
			text05 = text05 << 08 | text05 >> (64-08);
			text05 ^= text04;
			// Mix text06 text07 47
			text07 += key04;
			text06 += text07 + key03;
			text07 = text07 << 47 | text07 >> (64-47);
			text07 ^= text06;
			// Mix text08 text09 08
			text09 += key06;
			text08 += text09 + key05;
			text09 = text09 << 08 | text09 >> (64-08);
			text09 ^= text08;
			// Mix text10 text11 17
			text11 += key08;
			text10 += text11 + key07;
			text11 = text11 << 17 | text11 >> (64-17);
			text11 ^= text10;
			// Mix text12 text13 22
			text13 += key10;
			text12 += text13 + key09;
			text13 = text13 << 22 | text13 >> (64-22);
			text13 ^= text12;
			// Mix text14 text15 37
			text15 += key12 + 14;
			text14 += text15 + key11 + tweak00;
			text15 = text15 << 37 | text15 >> (64-37);
			text15 ^= text14;
			
			// round 57
			// Mix text00 text09 38
			text00 += text09;
			text09 = text09 << 38 | text09 >> (64-38);
			text09 ^= text00;
			// Mix text02 text13 19
			text02 += text13;
			text13 = text13 << 19 | text13 >> (64-19);
			text13 ^= text02;
			// Mix text06 text11 10
			text06 += text11;
			text11 = text11 << 10 | text11 >> (64-10);
			text11 ^= text06;
			// Mix text04 text15 55
			text04 += text15;
			text15 = text15 << 55 | text15 >> (64-55);
			text15 ^= text04;
			// Mix text10 text07 49
			text10 += text07;
			text07 = text07 << 49 | text07 >> (64-49);
			text07 ^= text10;
			// Mix text12 text03 18
			text12 += text03;
			text03 = text03 << 18 | text03 >> (64-18);
			text03 ^= text12;
			// Mix text14 text05 23
			text14 += text05;
			text05 = text05 << 23 | text05 >> (64-23);
			text05 ^= text14;
			// Mix text08 text01 52
			text08 += text01;
			text01 = text01 << 52 | text01 >> (64-52);
			text01 ^= text08;
			
			// round 58
			// Mix text00 text07 33
			text00 += text07;
			text07 = text07 << 33 | text07 >> (64-33);
			text07 ^= text00;
			// Mix text02 text05 04
			text02 += text05;
			text05 = text05 << 04 | text05 >> (64-04);
			text05 ^= text02;
			// Mix text04 text03 51
			text04 += text03;
			text03 = text03 << 51 | text03 >> (64-51);
			text03 ^= text04;
			// Mix text06 text01 13
			text06 += text01;
			text01 = text01 << 13 | text01 >> (64-13);
			text01 ^= text06;
			// Mix text12 text15 34
			text12 += text15;
			text15 = text15 << 34 | text15 >> (64-34);
			text15 ^= text12;
			// Mix text14 text13 41
			text14 += text13;
			text13 = text13 << 41 | text13 >> (64-41);
			text13 ^= text14;
			// Mix text08 text11 59
			text08 += text11;
			text11 = text11 << 59 | text11 >> (64-59);
			text11 ^= text08;
			// Mix text10 text09 17
			text10 += text09;
			text09 = text09 << 17 | text09 >> (64-17);
			text09 ^= text10;
			
			// round 59
			// Mix text00 text15 05
			text00 += text15;
			text15 = text15 << 05 | text15 >> (64-05);
			text15 ^= text00;
			// Mix text02 text11 20
			text02 += text11;
			text11 = text11 << 20 | text11 >> (64-20);
			text11 ^= text02;
			// Mix text06 text13 48
			text06 += text13;
			text13 = text13 << 48 | text13 >> (64-48);
			text13 ^= text06;
			// Mix text04 text09 41
			text04 += text09;
			text09 = text09 << 41 | text09 >> (64-41);
			text09 ^= text04;
			// Mix text14 text01 47
			text14 += text01;
			text01 = text01 << 47 | text01 >> (64-47);
			text01 ^= text14;
			// Mix text08 text05 28
			text08 += text05;
			text05 = text05 << 28 | text05 >> (64-28);
			text05 ^= text08;
			// Mix text10 text03 16
			text10 += text03;
			text03 = text03 << 16 | text03 >> (64-16);
			text03 ^= text10;
			// Mix text12 text07 25
			text12 += text07;
			text07 = text07 << 25 | text07 >> (64-25);
			text07 ^= text12;
			
			// round 60
			// Mix text00 text01 41
			text01 += key16;
			text00 += text01 + key15;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text00;
			// Mix text02 text03 09
			text03 += key01;
			text02 += text03 + key00;
			text03 = text03 << 09 | text03 >> (64-09);
			text03 ^= text02;
			// Mix text04 text05 37
			text05 += key03;
			text04 += text05 + key02;
			text05 = text05 << 37 | text05 >> (64-37);
			text05 ^= text04;
			// Mix text06 text07 31
			text07 += key05;
			text06 += text07 + key04;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text06;
			// Mix text08 text09 12
			text09 += key07;
			text08 += text09 + key06;
			text09 = text09 << 12 | text09 >> (64-12);
			text09 ^= text08;
			// Mix text10 text11 47
			text11 += key09;
			text10 += text11 + key08;
			text11 = text11 << 47 | text11 >> (64-47);
			text11 ^= text10;
			// Mix text12 text13 44
			text13 += key11;
			text12 += text13 + key10;
			text13 = text13 << 44 | text13 >> (64-44);
			text13 ^= text12;
			// Mix text14 text15 30
			text15 += key13 + 15;
			text14 += text15 + key12 + tweak01;
			text15 = text15 << 30 | text15 >> (64-30);
			text15 ^= text14;
			
			// round 61
			// Mix text00 text09 16
			text00 += text09;
			text09 = text09 << 16 | text09 >> (64-16);
			text09 ^= text00;
			// Mix text02 text13 34
			text02 += text13;
			text13 = text13 << 34 | text13 >> (64-34);
			text13 ^= text02;
			// Mix text06 text11 56
			text06 += text11;
			text11 = text11 << 56 | text11 >> (64-56);
			text11 ^= text06;
			// Mix text04 text15 51
			text04 += text15;
			text15 = text15 << 51 | text15 >> (64-51);
			text15 ^= text04;
			// Mix text10 text07 04
			text10 += text07;
			text07 = text07 << 04 | text07 >> (64-04);
			text07 ^= text10;
			// Mix text12 text03 53
			text12 += text03;
			text03 = text03 << 53 | text03 >> (64-53);
			text03 ^= text12;
			// Mix text14 text05 42
			text14 += text05;
			text05 = text05 << 42 | text05 >> (64-42);
			text05 ^= text14;
			// Mix text08 text01 41
			text08 += text01;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text08;
			
			// round 62
			// Mix text00 text07 31
			text00 += text07;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text00;
			// Mix text02 text05 44
			text02 += text05;
			text05 = text05 << 44 | text05 >> (64-44);
			text05 ^= text02;
			// Mix text04 text03 47
			text04 += text03;
			text03 = text03 << 47 | text03 >> (64-47);
			text03 ^= text04;
			// Mix text06 text01 46
			text06 += text01;
			text01 = text01 << 46 | text01 >> (64-46);
			text01 ^= text06;
			// Mix text12 text15 19
			text12 += text15;
			text15 = text15 << 19 | text15 >> (64-19);
			text15 ^= text12;
			// Mix text14 text13 42
			text14 += text13;
			text13 = text13 << 42 | text13 >> (64-42);
			text13 ^= text14;
			// Mix text08 text11 44
			text08 += text11;
			text11 = text11 << 44 | text11 >> (64-44);
			text11 ^= text08;
			// Mix text10 text09 25
			text10 += text09;
			text09 = text09 << 25 | text09 >> (64-25);
			text09 ^= text10;
			
			// round 63
			// Mix text00 text15 09
			text00 += text15;
			text15 = text15 << 09 | text15 >> (64-09);
			text15 ^= text00;
			// Mix text02 text11 48
			text02 += text11;
			text11 = text11 << 48 | text11 >> (64-48);
			text11 ^= text02;
			// Mix text06 text13 35
			text06 += text13;
			text13 = text13 << 35 | text13 >> (64-35);
			text13 ^= text06;
			// Mix text04 text09 52
			text04 += text09;
			text09 = text09 << 52 | text09 >> (64-52);
			text09 ^= text04;
			// Mix text14 text01 23
			text14 += text01;
			text01 = text01 << 23 | text01 >> (64-23);
			text01 ^= text14;
			// Mix text08 text05 31
			text08 += text05;
			text05 = text05 << 31 | text05 >> (64-31);
			text05 ^= text08;
			// Mix text10 text03 37
			text10 += text03;
			text03 = text03 << 37 | text03 >> (64-37);
			text03 ^= text10;
			// Mix text12 text07 20
			text12 += text07;
			text07 = text07 << 20 | text07 >> (64-20);
			text07 ^= text12;
			
			// round 64
			// Mix text00 text01 24
			text01 += key00;
			text00 += text01 + key16;
			text01 = text01 << 24 | text01 >> (64-24);
			text01 ^= text00;
			// Mix text02 text03 13
			text03 += key02;
			text02 += text03 + key01;
			text03 = text03 << 13 | text03 >> (64-13);
			text03 ^= text02;
			// Mix text04 text05 08
			text05 += key04;
			text04 += text05 + key03;
			text05 = text05 << 08 | text05 >> (64-08);
			text05 ^= text04;
			// Mix text06 text07 47
			text07 += key06;
			text06 += text07 + key05;
			text07 = text07 << 47 | text07 >> (64-47);
			text07 ^= text06;
			// Mix text08 text09 08
			text09 += key08;
			text08 += text09 + key07;
			text09 = text09 << 08 | text09 >> (64-08);
			text09 ^= text08;
			// Mix text10 text11 17
			text11 += key10;
			text10 += text11 + key09;
			text11 = text11 << 17 | text11 >> (64-17);
			text11 ^= text10;
			// Mix text12 text13 22
			text13 += key12;
			text12 += text13 + key11;
			text13 = text13 << 22 | text13 >> (64-22);
			text13 ^= text12;
			// Mix text14 text15 37
			text15 += key14 + 16;
			text14 += text15 + key13 + tweak02;
			text15 = text15 << 37 | text15 >> (64-37);
			text15 ^= text14;
			
			// round 65
			// Mix text00 text09 38
			text00 += text09;
			text09 = text09 << 38 | text09 >> (64-38);
			text09 ^= text00;
			// Mix text02 text13 19
			text02 += text13;
			text13 = text13 << 19 | text13 >> (64-19);
			text13 ^= text02;
			// Mix text06 text11 10
			text06 += text11;
			text11 = text11 << 10 | text11 >> (64-10);
			text11 ^= text06;
			// Mix text04 text15 55
			text04 += text15;
			text15 = text15 << 55 | text15 >> (64-55);
			text15 ^= text04;
			// Mix text10 text07 49
			text10 += text07;
			text07 = text07 << 49 | text07 >> (64-49);
			text07 ^= text10;
			// Mix text12 text03 18
			text12 += text03;
			text03 = text03 << 18 | text03 >> (64-18);
			text03 ^= text12;
			// Mix text14 text05 23
			text14 += text05;
			text05 = text05 << 23 | text05 >> (64-23);
			text05 ^= text14;
			// Mix text08 text01 52
			text08 += text01;
			text01 = text01 << 52 | text01 >> (64-52);
			text01 ^= text08;
			
			// round 66
			// Mix text00 text07 33
			text00 += text07;
			text07 = text07 << 33 | text07 >> (64-33);
			text07 ^= text00;
			// Mix text02 text05 04
			text02 += text05;
			text05 = text05 << 04 | text05 >> (64-04);
			text05 ^= text02;
			// Mix text04 text03 51
			text04 += text03;
			text03 = text03 << 51 | text03 >> (64-51);
			text03 ^= text04;
			// Mix text06 text01 13
			text06 += text01;
			text01 = text01 << 13 | text01 >> (64-13);
			text01 ^= text06;
			// Mix text12 text15 34
			text12 += text15;
			text15 = text15 << 34 | text15 >> (64-34);
			text15 ^= text12;
			// Mix text14 text13 41
			text14 += text13;
			text13 = text13 << 41 | text13 >> (64-41);
			text13 ^= text14;
			// Mix text08 text11 59
			text08 += text11;
			text11 = text11 << 59 | text11 >> (64-59);
			text11 ^= text08;
			// Mix text10 text09 17
			text10 += text09;
			text09 = text09 << 17 | text09 >> (64-17);
			text09 ^= text10;
			
			// round 67
			// Mix text00 text15 05
			text00 += text15;
			text15 = text15 << 05 | text15 >> (64-05);
			text15 ^= text00;
			// Mix text02 text11 20
			text02 += text11;
			text11 = text11 << 20 | text11 >> (64-20);
			text11 ^= text02;
			// Mix text06 text13 48
			text06 += text13;
			text13 = text13 << 48 | text13 >> (64-48);
			text13 ^= text06;
			// Mix text04 text09 41
			text04 += text09;
			text09 = text09 << 41 | text09 >> (64-41);
			text09 ^= text04;
			// Mix text14 text01 47
			text14 += text01;
			text01 = text01 << 47 | text01 >> (64-47);
			text01 ^= text14;
			// Mix text08 text05 28
			text08 += text05;
			text05 = text05 << 28 | text05 >> (64-28);
			text05 ^= text08;
			// Mix text10 text03 16
			text10 += text03;
			text03 = text03 << 16 | text03 >> (64-16);
			text03 ^= text10;
			// Mix text12 text07 25
			text12 += text07;
			text07 = text07 << 25 | text07 >> (64-25);
			text07 ^= text12;
			
			// round 68
			// Mix text00 text01 41
			text01 += key01;
			text00 += text01 + key00;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text00;
			// Mix text02 text03 09
			text03 += key03;
			text02 += text03 + key02;
			text03 = text03 << 09 | text03 >> (64-09);
			text03 ^= text02;
			// Mix text04 text05 37
			text05 += key05;
			text04 += text05 + key04;
			text05 = text05 << 37 | text05 >> (64-37);
			text05 ^= text04;
			// Mix text06 text07 31
			text07 += key07;
			text06 += text07 + key06;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text06;
			// Mix text08 text09 12
			text09 += key09;
			text08 += text09 + key08;
			text09 = text09 << 12 | text09 >> (64-12);
			text09 ^= text08;
			// Mix text10 text11 47
			text11 += key11;
			text10 += text11 + key10;
			text11 = text11 << 47 | text11 >> (64-47);
			text11 ^= text10;
			// Mix text12 text13 44
			text13 += key13;
			text12 += text13 + key12;
			text13 = text13 << 44 | text13 >> (64-44);
			text13 ^= text12;
			// Mix text14 text15 30
			text15 += key15 + 17;
			text14 += text15 + key14 + tweak00;
			text15 = text15 << 30 | text15 >> (64-30);
			text15 ^= text14;
			
			// round 69
			// Mix text00 text09 16
			text00 += text09;
			text09 = text09 << 16 | text09 >> (64-16);
			text09 ^= text00;
			// Mix text02 text13 34
			text02 += text13;
			text13 = text13 << 34 | text13 >> (64-34);
			text13 ^= text02;
			// Mix text06 text11 56
			text06 += text11;
			text11 = text11 << 56 | text11 >> (64-56);
			text11 ^= text06;
			// Mix text04 text15 51
			text04 += text15;
			text15 = text15 << 51 | text15 >> (64-51);
			text15 ^= text04;
			// Mix text10 text07 04
			text10 += text07;
			text07 = text07 << 04 | text07 >> (64-04);
			text07 ^= text10;
			// Mix text12 text03 53
			text12 += text03;
			text03 = text03 << 53 | text03 >> (64-53);
			text03 ^= text12;
			// Mix text14 text05 42
			text14 += text05;
			text05 = text05 << 42 | text05 >> (64-42);
			text05 ^= text14;
			// Mix text08 text01 41
			text08 += text01;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text08;
			
			// round 70
			// Mix text00 text07 31
			text00 += text07;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text00;
			// Mix text02 text05 44
			text02 += text05;
			text05 = text05 << 44 | text05 >> (64-44);
			text05 ^= text02;
			// Mix text04 text03 47
			text04 += text03;
			text03 = text03 << 47 | text03 >> (64-47);
			text03 ^= text04;
			// Mix text06 text01 46
			text06 += text01;
			text01 = text01 << 46 | text01 >> (64-46);
			text01 ^= text06;
			// Mix text12 text15 19
			text12 += text15;
			text15 = text15 << 19 | text15 >> (64-19);
			text15 ^= text12;
			// Mix text14 text13 42
			text14 += text13;
			text13 = text13 << 42 | text13 >> (64-42);
			text13 ^= text14;
			// Mix text08 text11 44
			text08 += text11;
			text11 = text11 << 44 | text11 >> (64-44);
			text11 ^= text08;
			// Mix text10 text09 25
			text10 += text09;
			text09 = text09 << 25 | text09 >> (64-25);
			text09 ^= text10;
			
			// round 71
			// Mix text00 text15 09
			text00 += text15;
			text15 = text15 << 09 | text15 >> (64-09);
			text15 ^= text00;
			// Mix text02 text11 48
			text02 += text11;
			text11 = text11 << 48 | text11 >> (64-48);
			text11 ^= text02;
			// Mix text06 text13 35
			text06 += text13;
			text13 = text13 << 35 | text13 >> (64-35);
			text13 ^= text06;
			// Mix text04 text09 52
			text04 += text09;
			text09 = text09 << 52 | text09 >> (64-52);
			text09 ^= text04;
			// Mix text14 text01 23
			text14 += text01;
			text01 = text01 << 23 | text01 >> (64-23);
			text01 ^= text14;
			// Mix text08 text05 31
			text08 += text05;
			text05 = text05 << 31 | text05 >> (64-31);
			text05 ^= text08;
			// Mix text10 text03 37
			text10 += text03;
			text03 = text03 << 37 | text03 >> (64-37);
			text03 ^= text10;
			// Mix text12 text07 20
			text12 += text07;
			text07 = text07 << 20 | text07 >> (64-20);
			text07 ^= text12;
			
			// round 72
			// Mix text00 text01 24
			text01 += key02;
			text00 += text01 + key01;
			text01 = text01 << 24 | text01 >> (64-24);
			text01 ^= text00;
			// Mix text02 text03 13
			text03 += key04;
			text02 += text03 + key03;
			text03 = text03 << 13 | text03 >> (64-13);
			text03 ^= text02;
			// Mix text04 text05 08
			text05 += key06;
			text04 += text05 + key05;
			text05 = text05 << 08 | text05 >> (64-08);
			text05 ^= text04;
			// Mix text06 text07 47
			text07 += key08;
			text06 += text07 + key07;
			text07 = text07 << 47 | text07 >> (64-47);
			text07 ^= text06;
			// Mix text08 text09 08
			text09 += key10;
			text08 += text09 + key09;
			text09 = text09 << 08 | text09 >> (64-08);
			text09 ^= text08;
			// Mix text10 text11 17
			text11 += key12;
			text10 += text11 + key11;
			text11 = text11 << 17 | text11 >> (64-17);
			text11 ^= text10;
			// Mix text12 text13 22
			text13 += key14;
			text12 += text13 + key13;
			text13 = text13 << 22 | text13 >> (64-22);
			text13 ^= text12;
			// Mix text14 text15 37
			text15 += key16 + 18;
			text14 += text15 + key15 + tweak01;
			text15 = text15 << 37 | text15 >> (64-37);
			text15 ^= text14;
			
			// round 73
			// Mix text00 text09 38
			text00 += text09;
			text09 = text09 << 38 | text09 >> (64-38);
			text09 ^= text00;
			// Mix text02 text13 19
			text02 += text13;
			text13 = text13 << 19 | text13 >> (64-19);
			text13 ^= text02;
			// Mix text06 text11 10
			text06 += text11;
			text11 = text11 << 10 | text11 >> (64-10);
			text11 ^= text06;
			// Mix text04 text15 55
			text04 += text15;
			text15 = text15 << 55 | text15 >> (64-55);
			text15 ^= text04;
			// Mix text10 text07 49
			text10 += text07;
			text07 = text07 << 49 | text07 >> (64-49);
			text07 ^= text10;
			// Mix text12 text03 18
			text12 += text03;
			text03 = text03 << 18 | text03 >> (64-18);
			text03 ^= text12;
			// Mix text14 text05 23
			text14 += text05;
			text05 = text05 << 23 | text05 >> (64-23);
			text05 ^= text14;
			// Mix text08 text01 52
			text08 += text01;
			text01 = text01 << 52 | text01 >> (64-52);
			text01 ^= text08;
			
			// round 74
			// Mix text00 text07 33
			text00 += text07;
			text07 = text07 << 33 | text07 >> (64-33);
			text07 ^= text00;
			// Mix text02 text05 04
			text02 += text05;
			text05 = text05 << 04 | text05 >> (64-04);
			text05 ^= text02;
			// Mix text04 text03 51
			text04 += text03;
			text03 = text03 << 51 | text03 >> (64-51);
			text03 ^= text04;
			// Mix text06 text01 13
			text06 += text01;
			text01 = text01 << 13 | text01 >> (64-13);
			text01 ^= text06;
			// Mix text12 text15 34
			text12 += text15;
			text15 = text15 << 34 | text15 >> (64-34);
			text15 ^= text12;
			// Mix text14 text13 41
			text14 += text13;
			text13 = text13 << 41 | text13 >> (64-41);
			text13 ^= text14;
			// Mix text08 text11 59
			text08 += text11;
			text11 = text11 << 59 | text11 >> (64-59);
			text11 ^= text08;
			// Mix text10 text09 17
			text10 += text09;
			text09 = text09 << 17 | text09 >> (64-17);
			text09 ^= text10;
			
			// round 75
			// Mix text00 text15 05
			text00 += text15;
			text15 = text15 << 05 | text15 >> (64-05);
			text15 ^= text00;
			// Mix text02 text11 20
			text02 += text11;
			text11 = text11 << 20 | text11 >> (64-20);
			text11 ^= text02;
			// Mix text06 text13 48
			text06 += text13;
			text13 = text13 << 48 | text13 >> (64-48);
			text13 ^= text06;
			// Mix text04 text09 41
			text04 += text09;
			text09 = text09 << 41 | text09 >> (64-41);
			text09 ^= text04;
			// Mix text14 text01 47
			text14 += text01;
			text01 = text01 << 47 | text01 >> (64-47);
			text01 ^= text14;
			// Mix text08 text05 28
			text08 += text05;
			text05 = text05 << 28 | text05 >> (64-28);
			text05 ^= text08;
			// Mix text10 text03 16
			text10 += text03;
			text03 = text03 << 16 | text03 >> (64-16);
			text03 ^= text10;
			// Mix text12 text07 25
			text12 += text07;
			text07 = text07 << 25 | text07 >> (64-25);
			text07 ^= text12;
			
			// round 76
			// Mix text00 text01 41
			text01 += key03;
			text00 += text01 + key02;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text00;
			// Mix text02 text03 09
			text03 += key05;
			text02 += text03 + key04;
			text03 = text03 << 09 | text03 >> (64-09);
			text03 ^= text02;
			// Mix text04 text05 37
			text05 += key07;
			text04 += text05 + key06;
			text05 = text05 << 37 | text05 >> (64-37);
			text05 ^= text04;
			// Mix text06 text07 31
			text07 += key09;
			text06 += text07 + key08;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text06;
			// Mix text08 text09 12
			text09 += key11;
			text08 += text09 + key10;
			text09 = text09 << 12 | text09 >> (64-12);
			text09 ^= text08;
			// Mix text10 text11 47
			text11 += key13;
			text10 += text11 + key12;
			text11 = text11 << 47 | text11 >> (64-47);
			text11 ^= text10;
			// Mix text12 text13 44
			text13 += key15;
			text12 += text13 + key14;
			text13 = text13 << 44 | text13 >> (64-44);
			text13 ^= text12;
			// Mix text14 text15 30
			text15 += key00 + 19;
			text14 += text15 + key16 + tweak02;
			text15 = text15 << 30 | text15 >> (64-30);
			text15 ^= text14;
			
			// round 77
			// Mix text00 text09 16
			text00 += text09;
			text09 = text09 << 16 | text09 >> (64-16);
			text09 ^= text00;
			// Mix text02 text13 34
			text02 += text13;
			text13 = text13 << 34 | text13 >> (64-34);
			text13 ^= text02;
			// Mix text06 text11 56
			text06 += text11;
			text11 = text11 << 56 | text11 >> (64-56);
			text11 ^= text06;
			// Mix text04 text15 51
			text04 += text15;
			text15 = text15 << 51 | text15 >> (64-51);
			text15 ^= text04;
			// Mix text10 text07 04
			text10 += text07;
			text07 = text07 << 04 | text07 >> (64-04);
			text07 ^= text10;
			// Mix text12 text03 53
			text12 += text03;
			text03 = text03 << 53 | text03 >> (64-53);
			text03 ^= text12;
			// Mix text14 text05 42
			text14 += text05;
			text05 = text05 << 42 | text05 >> (64-42);
			text05 ^= text14;
			// Mix text08 text01 41
			text08 += text01;
			text01 = text01 << 41 | text01 >> (64-41);
			text01 ^= text08;
			
			// round 78
			// Mix text00 text07 31
			text00 += text07;
			text07 = text07 << 31 | text07 >> (64-31);
			text07 ^= text00;
			// Mix text02 text05 44
			text02 += text05;
			text05 = text05 << 44 | text05 >> (64-44);
			text05 ^= text02;
			// Mix text04 text03 47
			text04 += text03;
			text03 = text03 << 47 | text03 >> (64-47);
			text03 ^= text04;
			// Mix text06 text01 46
			text06 += text01;
			text01 = text01 << 46 | text01 >> (64-46);
			text01 ^= text06;
			// Mix text12 text15 19
			text12 += text15;
			text15 = text15 << 19 | text15 >> (64-19);
			text15 ^= text12;
			// Mix text14 text13 42
			text14 += text13;
			text13 = text13 << 42 | text13 >> (64-42);
			text13 ^= text14;
			// Mix text08 text11 44
			text08 += text11;
			text11 = text11 << 44 | text11 >> (64-44);
			text11 ^= text08;
			// Mix text10 text09 25
			text10 += text09;
			text09 = text09 << 25 | text09 >> (64-25);
			text09 ^= text10;
			
			// round 79
			// Mix text00 text15 09
			text00 += text15;
			text15 = text15 << 09 | text15 >> (64-09);
			text15 ^= text00;
			// Mix text02 text11 48
			text02 += text11;
			text11 = text11 << 48 | text11 >> (64-48);
			text11 ^= text02;
			// Mix text06 text13 35
			text06 += text13;
			text13 = text13 << 35 | text13 >> (64-35);
			text13 ^= text06;
			// Mix text04 text09 52
			text04 += text09;
			text09 = text09 << 52 | text09 >> (64-52);
			text09 ^= text04;
			// Mix text14 text01 23
			text14 += text01;
			text01 = text01 << 23 | text01 >> (64-23);
			text01 ^= text14;
			// Mix text08 text05 31
			text08 += text05;
			text05 = text05 << 31 | text05 >> (64-31);
			text05 ^= text08;
			// Mix text10 text03 37
			text10 += text03;
			text03 = text03 << 37 | text03 >> (64-37);
			text03 ^= text10;
			// Mix text12 text07 20
			text12 += text07;
			text07 = text07 << 20 | text07 >> (64-20);
			text07 ^= text12;
			
			// Final
			text00 += key03;
			text01 += key04;
			text02 += key05;
			text03 += key06;
			text04 += key07;
			text05 += key08;
			text06 += key09;
			text07 += key10;
			text08 += key11;
			text09 += key12;
			text10 += key13;
			text11 += key14;
			text12 += key15;
			text13 += key16 + tweak02;
			text14 += key00  + tweak00;
			text15 += key01 + 20;
		}
		
	}
	
}

