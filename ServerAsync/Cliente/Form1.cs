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
using System.Net.Sockets;
using System.Net;

namespace Cliente
{
        /********************************
         *  Aplicaciones Distribuidas   *
         *  Francisco Gualli            *
         *  Gr1                         *
         *  Practica 02                 *
         *  Cliente                     *
         /*******************************/
    public partial class FrmLogin : Form
    {
        private Socket socketCliente; 
        private string nombre; 
        private EndPoint epServidor; 
        private byte[] buferRx = new byte[1024];
        private delegate void DelegadoMensajeActualizacion(string mensaje); 
        private DelegadoMensajeActualizacion delegadoActualizacion = null;

        public FrmLogin()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            delegadoActualizacion = new DelegadoMensajeActualizacion(DesplegarMensaje);
        }

        private void btnIngresar_Click(object sender, EventArgs e)
        {
            try
            {
                nombre = txtNombre.Text.Trim();
                Paquete paqueteInicio = new Paquete();
                paqueteInicio.NombreChat = nombre;
                paqueteInicio.MensajeChat = null;
                paqueteInicio.IdentificadorChat = Paquete.IdentificadorDato.Conectado;
                socketCliente = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPAddress servidorIP = IPAddress.Parse(txtServidor.Text.Trim());
                IPEndPoint puntoRemoto = new IPEndPoint(servidorIP, 30000);
                epServidor = (EndPoint)puntoRemoto;
                byte[] buferTx = paqueteInicio.ObtenerArregloBytes();
                socketCliente.BeginSendTo(buferTx, 0, buferTx.Length, SocketFlags.None, epServidor, new AsyncCallback(ProcesarEnviar), null);
                buferRx = new byte[1024];
                socketCliente.BeginReceiveFrom(buferRx, 0, buferRx.Length, SocketFlags.None, ref epServidor, new AsyncCallback(this.ProcesarRecibir), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectarse: " + ex.Message, "Cliente UDP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            try
            {
                Paquete paqueteParaEnviar = new Paquete(); 
                paqueteParaEnviar.NombreChat = nombre; 
                paqueteParaEnviar.MensajeChat = txtMensajeParaEnviar.Text.Trim(); 
                paqueteParaEnviar.IdentificadorChat = Paquete.IdentificadorDato.Mensaje;
                byte[] arregloBytes = paqueteParaEnviar.ObtenerArregloBytes();
                socketCliente.BeginSendTo(arregloBytes, 0, arregloBytes.Length, SocketFlags.None, epServidor, new AsyncCallback(ProcesarEnviar), null);
                txtMensajeParaEnviar.Text = string.Empty;
            }
            catch (Exception ex) { MessageBox.Show("Error al enviar: " + ex.Message, "Cliente UDP", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ProcesarEnviar(IAsyncResult res) 
        {
            try
            {
                socketCliente.EndSend(res);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Enviar Datos: " + ex.Message, "Cliente UDP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ProcesarRecibir(IAsyncResult res)
        {
            try
            {
                socketCliente.EndReceive(res); Paquete paqueteRecibido = new Paquete(buferRx);
                if (paqueteRecibido.MensajeChat != null)
                    Invoke(delegadoActualizacion, new object[] { paqueteRecibido.MensajeChat });
                buferRx = new byte[1024];
                socketCliente.BeginReceiveFrom(buferRx, 0, buferRx.Length, SocketFlags.None, ref epServidor, new AsyncCallback(ProcesarRecibir), null);
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex) { MessageBox.Show("Datos Recibidos: " + ex.Message, "Cliente UDP", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        private void DesplegarMensaje(string mensaje)
        {
            rxtMensajes.Text += mensaje + Environment.NewLine;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (this.socketCliente != null)
                {
                    Paquete paqueteSalida = new Paquete(); 
                    paqueteSalida.IdentificadorChat = Paquete.IdentificadorDato.Desconectado; 
                    paqueteSalida.NombreChat = nombre; paqueteSalida.MensajeChat = null;
                    byte[] buferTx = paqueteSalida.ObtenerArregloBytes();
                    socketCliente.SendTo(buferTx, 0, buferTx.Length, SocketFlags.None, epServidor);
                    socketCliente.Close();
                }
            }
            catch (Exception ex) { MessageBox.Show("Error al desconectar: " + ex.Message, "Cliente UDP", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void txtServidor_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar.Equals((char)Keys.Enter))
            {
                btnIngresar_Click(sender,e);                
            }
        }

        private void txtMensajeParaEnviar_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar.Equals((char)Keys.Enter))
            {
                btnEnviar_Click(sender,e);
            }
        }
    }
}
