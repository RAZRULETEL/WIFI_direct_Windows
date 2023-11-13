//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Devices.Enumeration;
using System.ComponentModel;
using Windows.Devices.WiFiDirect;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Core;
using Windows.Foundation;
using System.Diagnostics;

namespace SDKTemplate
{
    public class SocketReaderWriter : IDisposable
    {
        DataReader _dataReader;
        DataWriter _dataWriter;
        StreamSocket _streamSocket;


        public SocketReaderWriter(StreamSocket socket)
        {
            _dataReader = new DataReader(socket.InputStream);
            _dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
            _dataReader.ByteOrder = ByteOrder.LittleEndian;

            _dataWriter = new DataWriter(socket.OutputStream);
            _dataWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
            _dataWriter.ByteOrder = ByteOrder.LittleEndian;

            _streamSocket = socket;
        }

        public void Dispose()
        {
            _dataReader.Dispose();
            _dataWriter.Dispose();
            _streamSocket.Dispose();
        }

        public async Task WriteMessageAsync(string message)
        {
            try
            {
                _dataWriter.WriteUInt32(_dataWriter.MeasureString(message));
                _dataWriter.WriteString(message);
                await _dataWriter.StoreAsync();
                //_rootPage?.NotifyUserFromBackground("Sent message: " + message, NotifyType.StatusMessage);
                Debug.WriteLine("Sent message: " + message);


            }
            catch (Exception ex)
            {
                //_rootPage?.NotifyUserFromBackground("WriteMessage threw exception: " + ex.Message, NotifyType.StatusMessage);
                Debug.WriteLine("WriteMessage threw exception: " + ex.Message);
            }
        }

        public async Task<string> ReadMessageAsync()
        {
            try
            {
                UInt32 bytesRead = await _dataReader.LoadAsync(sizeof(UInt32));
                if (bytesRead > 0)
                {
                    // Determine how long the string is.
                    UInt32 messageLength = _dataReader.ReadUInt32();
                    Debug.WriteLine("Message size is " + messageLength);
                    bytesRead = await _dataReader.LoadAsync(messageLength);
                    Debug.WriteLine("Bytes read " + messageLength);
                    if (bytesRead > 0)
                    {
                        // Decode the string.
                        string message = _dataReader.ReadString(messageLength);
                        //_rootPage?.NotifyUserFromBackground("Got message: " + message, NotifyType.StatusMessage);
                        Debug.WriteLine("Got message: " + message);
                        return message;
                    }
                    else
                    {
                        Debug.WriteLine("Received zero length message");
                    }
                }
                
            }
            catch (Exception)
            {
                Debug.WriteLine("Socket was closed!");
                //_rootPage?.NotifyUserFromBackground("Socket was closed!", NotifyType.StatusMessage);
            }
            return null;
        }
    }
}
