﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;

namespace MQTTnet.Core.Server
{
    public class MqttServer
    {
        private readonly MqttClientSessionsManager _clientSessionsManager;
        private readonly IMqttServerAdapter _adapter;
        private readonly MqttServerOptions _options;

        private CancellationTokenSource _cancellationTokenSource;

        public MqttServer(MqttServerOptions options, IMqttServerAdapter adapter)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            
            _clientSessionsManager = new MqttClientSessionsManager(options);
        }

        public IList<string> GetConnectedClients()
        {
            return _clientSessionsManager.GetConnectedClients();
        }

        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;

        public void InjectClient(string identifier, IMqttCommunicationAdapter adapter)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            if (_cancellationTokenSource == null) throw new InvalidOperationException("The MQTT server is not started.");

            OnClientConnected(this, new MqttClientConnectedEventArgs(identifier, adapter));
        }

        public void Start()
        {
            if (_cancellationTokenSource != null) throw new InvalidOperationException("The MQTT server is already started.");

            _cancellationTokenSource = new CancellationTokenSource();

            _adapter.ClientConnected += OnClientConnected;
            _adapter.Start(_options);

            MqttTrace.Information(nameof(MqttServer), "Started.");
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;

            _adapter.ClientConnected -= OnClientConnected;
            _adapter.Stop();

            _clientSessionsManager.Clear();

            MqttTrace.Information(nameof(MqttServer), "Stopped.");
        }

        private void OnClientConnected(object sender, MqttClientConnectedEventArgs eventArgs)
        {
            MqttTrace.Information(nameof(MqttServer), $"Client '{eventArgs.Identifier}': Connected.");
            ClientConnected?.Invoke(this, eventArgs);

            Task.Run(() => _clientSessionsManager.RunClientSessionAsync(eventArgs), _cancellationTokenSource.Token);
        }
    }
}
