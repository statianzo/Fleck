using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Nugget.Tests
{
    class ExtendedClientHandshake : ClientHandshake
    {

        class Key
        {
            private static Random rand = new Random(DateTime.Now.Millisecond);
            public int Spaces { get; set; }
            public long Digit { get; set; }
            private string key;
            public Key()
            {
                var done = Generate();
                while (!done)
                    Generate();
            }

            private bool Generate()
            {
                try
                {
                    var length = rand.Next(10, 20);
                    var digits = "";

                    for (int i = 0; i < length; i++)
                    {
                        var spaceDigitChar = rand.Next(3);
                        switch (spaceDigitChar)
                        {
                            case 0: // space
                                key += " ";
                                Spaces++;
                                break;
                            case 1: // digit
                                var digit = rand.Next(10);
                                key += digit.ToString();
                                digits += digit.ToString();
                                break;
                            case 2: // char
                                var c = (char)rand.Next(256);
                                while(!Char.IsLetter(c))
                                    c = (char)rand.Next(256);

                                key += c;
                                break;
                        }
                    }
                    Digit = Int64.Parse(digits);
                    if (Spaces == 0) // make sure we atleast have one space (to avoid divide by zero later)
                    {
                        key += " ";
                        Spaces += 1;
                    }

                    return true;
                }
                catch
                {
                    return false;
                }

            }

            public override string ToString()
            {
                return key;
            }
        }

        public byte[] ExpectedAnswer { get; set; }

        public ExtendedClientHandshake()
        {
            var k1 = new Key();
            var k2 = new Key();

            var ch = new byte[8];
            var rand = new Random(DateTime.Now.Millisecond);
            rand.NextBytes(ch);
            
            // divide the digits with the number of spaces
            Int32 r1 = (Int32)(k1.Digit / k1.Spaces);
            Int32 r2 = (Int32)(k2.Digit / k2.Spaces);

            // convert the results to 32 bit big endian byte arrays
            byte[] rb1 = BitConverter.GetBytes(r1);
            byte[] rb2 = BitConverter.GetBytes(r2);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(rb1);
                Array.Reverse(rb2);
            }

            // concat the two integers and the 8 challenge bytes from the client
            byte[] ra = new byte[16];
            Array.Copy(rb1, 0, ra, 0, 4);
            Array.Copy(rb2, 0, ra, 4, 4);
            Array.Copy(ch, 0, ra, 8, 8);

            // compute the md5 hash
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            
            ExpectedAnswer = md5.ComputeHash(ra);
            ChallengeBytes = new ArraySegment<byte>(ch);
            Key1 = k1.ToString();
            Key2 = k2.ToString();
        }
    }
}
