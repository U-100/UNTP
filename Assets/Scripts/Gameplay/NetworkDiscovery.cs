using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace UNTP
{
	public enum DiscoveryMessageType : byte
	{
		DiscoveryRequest = 1,
		DiscoveryResponse = 2,
	}

	public class NetworkDiscovery
	{
		private readonly long _appId;
		private readonly ushort _port;

		public NetworkDiscovery(long appId, ushort port)
		{
			this._appId = appId;
			this._port = port;
		}

		public async IAsyncEnumerable<IPEndPoint> FindServers([EnumeratorCancellation] CancellationToken ct = default)
		{
			using UdpClient udpClient = new UdpClient(0) { EnableBroadcast = true, MulticastLoopback = false };

			IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, this._port);

			byte[] data = null;
			using (FastBufferWriter writer = new FastBufferWriter(16, Allocator.Temp, 64))
			{
				WritePacket(writer, DiscoveryMessageType.DiscoveryRequest);
				data = writer.ToArray();
			}

			await udpClient.SendAsync(data, data.Length, endPoint);

			using var cancellationTokenRegistration = ct.Register(() => udpClient.Close());

			while (true)
			{
				UdpReceiveResult udpReceiveResult;
				try { udpReceiveResult = await udpClient.ReceiveAsync(); }
				catch (ObjectDisposedException) { yield break; } // socket has been closed, i.e. cancellation has been requested

				ArraySegment<byte> segment = new ArraySegment<byte>(udpReceiveResult.Buffer, 0, udpReceiveResult.Buffer.Length);

				bool readAndCheckHeaderResult;
				using (FastBufferReader reader = new FastBufferReader(segment, Allocator.Temp))
				{
					readAndCheckHeaderResult = ReadAndCheckPacket(reader, DiscoveryMessageType.DiscoveryResponse);
				}

				if (readAndCheckHeaderResult)
				{
					Debug.Log($"Discovered server at {udpReceiveResult.RemoteEndPoint}");

					yield return udpReceiveResult.RemoteEndPoint;
				}
			}
		}

		public async Task AdvertiseServer(CancellationToken ct = default)
		{
			Debug.Log($"Starting server discovery listener on port {this._port}");

			using UdpClient udpClient = new UdpClient(this._port) { EnableBroadcast = true, MulticastLoopback = false };
			
			while(true)
			{
				ct.ThrowIfCancellationRequested();

				UdpReceiveResult udpReceiveResult;

				try
				{
					using (ct.Register(() => udpClient.Close()))
						udpReceiveResult = await udpClient.ReceiveAsync();
				}
				catch (Exception)
				{
					if (ct.IsCancellationRequested)
						throw new OperationCanceledException();

					throw;
				}

				ArraySegment<byte> segment = new ArraySegment<byte>(udpReceiveResult.Buffer, 0, udpReceiveResult.Buffer.Length);

				bool readAndCheckHeaderResult;
				using (FastBufferReader reader = new FastBufferReader(segment, Allocator.Temp))
				{
					readAndCheckHeaderResult = ReadAndCheckPacket(reader, DiscoveryMessageType.DiscoveryRequest);
				}

				if (readAndCheckHeaderResult)
				{
					byte[] data;
					using (FastBufferWriter writer = new FastBufferWriter(16, Allocator.Temp, 64))
					{
						WritePacket(writer, DiscoveryMessageType.DiscoveryResponse);
						data = writer.ToArray();
					}

					Debug.Log($"Responding to a discovery broadcast from {udpReceiveResult.RemoteEndPoint}");

					await udpClient.SendAsync(data, data.Length, udpReceiveResult.RemoteEndPoint);
				}
			}
		}

		private void WritePacket(FastBufferWriter writer, DiscoveryMessageType discoveryMessageType)
		{
			// Serialize unique application id to make sure packet received is from same application.
			writer.WriteValueSafe(this._appId);

			// Write a flag indicating whether this is a broadcast
			writer.WriteByteSafe((byte)discoveryMessageType);
		}

		private bool ReadAndCheckPacket(FastBufferReader reader, DiscoveryMessageType expectedType)
		{
			reader.ReadValueSafe(out long receivedApplicationId);
			if (receivedApplicationId != this._appId)
				return false;

			reader.ReadByteSafe(out byte messageType);
			if (messageType != (byte)expectedType)
				return false;

			return true;
		}
	}
}
