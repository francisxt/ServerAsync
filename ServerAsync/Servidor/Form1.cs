using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Protocolo;
using System.Net;
using System.Net.Sockets;
using System.Collections;

namespace Servidor
{
        /********************************
         *  Aplicaciones Distribuidas   *
         *  Francisco Gualli            *
         *  Gr1                         *
         *  Practica 02                 *
         *  Servidor                    *    
         /*******************************/
    public partial class Form1 : Form
    {
        private struct Cliente 
        { 
            public EndPoint puntoExtremo; 
            public string nombre; 
        }
        private ArrayList listaClientes;
        private Socket socketServidor;
        public byte[] buferRx = new byte[1024];
        private delegate void DelegadoActualizarEstado(string estado); 
        private DelegadoActualizarEstado delegadoActualizarEstado = null;
        public Form1()
        {
            InitializeComponent();
        }

        private void btnTerminar_Click(object sender, EventArgs e)
        {
            socketServidor.Close(); 
            Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            try
            {
                listaClientes = new ArrayList();
                delegadoActualizarEstado = new DelegadoActualizarEstado(ActualizarEstado);
                socketServidor = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint servidorExtremo = new IPEndPoint(IPAddress.Any, 30000);
                socketServidor.Bind(servidorExtremo);
                IPEndPoint clienteExtremo = new IPEndPoint(IPAddress.Any, 0);
                EndPoint extremoEP = (EndPoint)clienteExtremo;
                socketServidor.BeginReceiveFrom(buferRx, 0, buferRx.Length, SocketFlags.None, ref extremoEP, new AsyncCallback(ProcesarRecibir), extremoEP);
                lbEstado.Text = "Escuchando";
            }
            catch (Exception ex)
            {
                lbEstado.Text = "Error"; MessageBox.Show("Cargando Error: " + ex.Message,"Servidor UDP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void ProcesarRecibir(IAsyncResult resultadoAsync)
        {
            try
            {
                byte[] data;                
                Paquete datoRecibido = new Paquete(buferRx);
                Paquete datoParaEnviar = new Paquete();                
                IPEndPoint puntoExtremoCliente = new IPEndPoint(IPAddress.Any, 0);
                EndPoint extremoEP = (EndPoint)puntoExtremoCliente;
                socketServidor.EndReceiveFrom(resultadoAsync, ref extremoEP);
                datoParaEnviar.IdentificadorChat = datoRecibido.IdentificadorChat; 
                datoParaEnviar.NombreChat = datoRecibido.NombreChat;
                switch (datoRecibido.IdentificadorChat)
                {
                    case Paquete.IdentificadorDato.Mensaje:
                        datoParaEnviar.MensajeChat = string.Format("{0}: {1}", datoRecibido.NombreChat, datoRecibido.MensajeChat);
                        break;
                    case Paquete.IdentificadorDato.Conectado:
                        Cliente nuevoCliente = new Cliente();
                        nuevoCliente.puntoExtremo = extremoEP;
                        nuevoCliente.nombre = datoRecibido.NombreChat;
                        listaClientes.Add(nuevoCliente);
                        datoParaEnviar.MensajeChat = string.Format("-- {0} está conectado --", datoRecibido.NombreChat);
                        break;
                    case Paquete.IdentificadorDato.Desconectado:
                        foreach (Cliente c in listaClientes)
                        {
                            if (c.puntoExtremo.Equals(extremoEP))
                            {
                                listaClientes.Remove(c);
                                break;
                            }
                        }
                        datoParaEnviar.MensajeChat = string.Format("-- {0} se ha desconectado -- ", datoRecibido.NombreChat);
                        break;
                }
                data = datoParaEnviar.ObtenerArregloBytes();
                foreach (Cliente clienteEnLista in listaClientes)
                {
                    if (clienteEnLista.puntoExtremo != extremoEP || datoParaEnviar.IdentificadorChat != Paquete.IdentificadorDato.Conectado)
                    {
                        socketServidor.BeginSendTo(data, 0, data.Length, SocketFlags.None, clienteEnLista.puntoExtremo, new AsyncCallback(ProcesarEnviar), clienteEnLista.puntoExtremo);
                    }
                }
                socketServidor.BeginReceiveFrom(buferRx, 0, buferRx.Length, SocketFlags.None, ref extremoEP, new AsyncCallback(ProcesarRecibir), extremoEP);
                Invoke(delegadoActualizarEstado, new object[] { datoParaEnviar.MensajeChat });
            }
            catch (Exception ex) { MessageBox.Show("Error en la recepción: " + ex.Message, "Servidor UDP", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        public void ProcesarEnviar(IAsyncResult resultadoAsync)
        {
            try
            {
                socketServidor.EndSend(resultadoAsync);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al enviar datos: " + ex.Message, "Servidor UDP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ActualizarEstado(string estado)
        {
            rtxInformacion.Text += estado + Environment.NewLine;
        }
    }
}
