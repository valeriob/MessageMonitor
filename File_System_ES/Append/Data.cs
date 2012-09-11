﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace File_System_ES.Append
{
    public class Data
    {
        public int Key { get; set; }
        public DateTime Timestamp { get; set; }
        public int Version { get; set; }
        public byte[] Payload { get; set; }


        public byte[] To_Bytes()
        {
            var size = 4 + 8 + 4 + 4 + Payload.Length;
            var buffer = new byte[size];

            Array.Copy(BitConverter.GetBytes(size), 0, buffer, 0, 4);
            Array.Copy(BitConverter.GetBytes(Key), 0, buffer, 4, 4);
            Array.Copy(BitConverter.GetBytes(Timestamp.Ticks), 0, buffer, 8, 8);
            Array.Copy(BitConverter.GetBytes(Version), 0, buffer, 16, 4);
            Array.Copy(Payload, 0, buffer, 20, Payload.Length);

            return buffer;
        }

        public static Data From_Bytes(Stream stream)
        {
            var buffer = new byte[4];
            stream.Read(buffer, 0, 4);

            int lenght = BitConverter.ToInt32(buffer, 0);
            buffer = new byte[lenght];
            stream.Read(buffer, 0, lenght);

            return From_Bytes(buffer, lenght);
        }

        public static Data From_Bytes(byte[] buffer, int totalLenght)
        {
            var data = new Data
            {
                Key = BitConverter.ToInt32(buffer, 0),
                Timestamp = DateTime.FromBinary(BitConverter.ToInt64(buffer, 4)),
                Version = BitConverter.ToInt32(buffer, 12),
                Payload = new byte[totalLenght - 20]
            };

            Array.Copy(buffer, 16, data.Payload, 0, totalLenght - 20);

            return data;
        }


    }
}