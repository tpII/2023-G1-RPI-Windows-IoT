using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Sensors.Dht;
using Windows.Devices.Gpio;

using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;



// La plantilla de elemento Página en blanco está documentada en https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0xc0a

namespace SensorTemp
{
    /// <summary>
    /// Página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Dht11 _dht11;
        private GpioPin _gpioPin;
        private MqttClient _mqttClient;
        private string _topic;

        public MainPage()
        {
            this.InitializeComponent();
            InitializeSensor();
            InitializeMQTT();
        }

        private void InitializeMQTT()
        {
            // Configura el cliente MQTT
            _mqttClient = new MqttClient("mqtt.thingsboard.cloud", 1883, false, MqttSslProtocols.None); // Dirección de tu instancia de ThingsBoard
                                                                                                        // Conecta al broker MQTT
            _mqttClient.Connect("PC", "TDP2", "ROOT");
            _mqttClient.ProtocolVersion = MqttProtocolVersion.Version_3_1;
            // Configura el topic
            _topic = "v1/devices/me/telemetry";
        }

        private static void client_MqttMsgConnected(object sender, MqttMsgConnectEventArgs e)
        {
            Console.WriteLine("Conectado a ThingsBoard");

            // Aquí puedes agregar lógica adicional después de la conexión
        }

        private void InitializeSensor()
        {
            // Configura el número del pin GPIO que deseas utilizar
            int pinNumber = 4;
            // Inicializa el controlador GPIO
            GpioController gpioController = GpioController.GetDefault();
            if (gpioController != null)
            {
                // Abre el pin GPIO
                _gpioPin = gpioController.OpenPin(pinNumber);

                if (_gpioPin != null)
                {
                    // Inicializa el sensor DHT11 con el pin GPIO
                    _dht11 = new Dht11(_gpioPin, GpioPinDriveMode.Input);

                    // Configura un temporizador para leer datos del sensor periódicamente
                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(2); // Lee cada 2 segundos
                    timer.Tick += Timer_Tick;
                    timer.Start();
                }
                else
                {
                    // No se pudo abrir el pin GPIO
                }
            }
            else
            {
                // No se pudo obtener el controlador GPIO
            }
        }

        private async void Timer_Tick(object sender, object e)
        {
            // Lee datos del sensor DHT11
            DhtReading reading = await _dht11.GetReadingAsync().AsTask();
            
            if (reading.IsValid)
            {
                // Muestra los datos en la interfaz de usuario
                TemperatureTextBlock.Text = $"Temperatura: {reading.Temperature}°C";
                HumidityTextBlock.Text = $"Humedad: {reading.Humidity}%";

                string payload = $"{{ \"temperature\": {reading.Temperature}, \"humidity\": {reading.Humidity} }}";
                _mqttClient.Publish(_topic, Encoding.UTF8.GetBytes(payload), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
                Console.WriteLine($"Mensaje publicado en el topic: {_topic}");
            }
            else
            {
                // Los datos del sensor no son válidos
                TemperatureTextBlock.Text = "Error al leer la temperatura";
                HumidityTextBlock.Text = "Error al leer la humedad";
                string payload = $"{{ \"temperature\": 0, \"humidity\": 0 }}";
                _mqttClient.Publish(_topic, Encoding.UTF8.GetBytes(payload), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
            }
        }
    }
}

