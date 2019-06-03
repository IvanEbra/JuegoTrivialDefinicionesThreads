using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Threading;
using System.IO;
using System.Timers;

namespace Cliente
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //cliente para poder comunicarse co server
        TcpClient client;
        //variables para realizar a comunicacion
        NetworkStream ns;
        StreamWriter sw;
        StreamReader sr;
        String dato;
        String mensajes;

        TcpListener newSock;

        String puerto;

        private void BtnConectar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //creamos o cliente na ip e no porto do servidor
                client = new TcpClient(txtIpServidor.Text, 2000);
                ns = client.GetStream();
                sr = new StreamReader(ns);
                sw = new StreamWriter(ns);
                while (sr.Peek() > -1)
                {
                    dato += sr.ReadLine() + "\n";
                }
                richTxtMensaxesRecibidos.AppendText(dato + "\n");
                dato = "";

                btnConectar.IsEnabled = false;
            }
            catch (Exception er)
            {
                Console.WriteLine("ERROR :" + er.ToString());
            }
        }

        private void BtnInscribir_Click(object sender, RoutedEventArgs e)
        {
            puerto = txtPuertoRecibir.Text;
            //mandamos mensaxe de inscripcion segindo o protocolo
            sw.WriteLine("#INSCRIBIR#" + txtNickJugador.Text + "#" + puerto);
            sw.Flush();

            if (sr.EndOfStream == true)//truquiño para que o sr.peek non dea null e se faga despois de que lle chegue algo do servidor
            {
                Console.WriteLine("stream finalizado");
            }
            //lemos a resposta do servidor
            while (sr.Peek() > -1)
            {
                dato += sr.ReadLine() + "\n";
            }
            richTxtMensaxesRecibidos.AppendText(dato + "\n");

            btnInscribir.IsEnabled = false;

            //abrimos un fio para recibir os mensaxes que mande o servidor en bradcasting
            Thread tEscucharServer = new Thread(EscucharServer);
            tEscucharServer.IsBackground = true;//pomos o fio como proceso en segundo plano
            tEscucharServer.Start();

            //abrimos un fio para recibir os mensaxes que mande o servidor en bradcasting
            Thread tEscucharResultado = new Thread(EscucharResultado);
            tEscucharResultado.IsBackground = true;//pomos o fio como proceso en segundo plano
            tEscucharResultado.Start();
        }


        delegate void DelegadoRespuesta();

        private void EscucharServer()
        {
            int puer = System.Convert.ToInt32(puerto);
            //creamos un listenner no porto que lle mandamos ao server e ao que enviara os mensaxes
            newSock = new TcpListener(IPAddress.Any, puer);
            newSock.Start();
            Console.WriteLine("Esperando al servidor");

            //creamos un bucle infinito para que reciba constantemente as conexions do server 
            while (true)
            {
                TcpClient client = newSock.AcceptTcpClient();//linea bloqueante ata que non reciba un cliente
                NetworkStream ns = client.GetStream();
                StreamWriter sw = new StreamWriter(ns);
                StreamReader sr = new StreamReader(ns);

                if (sr.EndOfStream == true)
                {
                    Console.WriteLine("stream finalizado");
                }
                while (sr.Peek() > -1)
                {
                    dato += sr.ReadLine() + "\n";// liniea bloqueante ata que lle chegue algo do servidor
                }


                //chamamos ao metodo para escribir nun delegado por si acaso
                DelegadoRespuesta dr = new DelegadoRespuesta(EscribirRecibido);
                Dispatcher.Invoke(dr);

                dato = "";
            }
        }

        private void EscribirRecibido()
        {
            richTxtMensaxesRecibidos.AppendText(dato + "\n");
            richTxtMensaxesRecibidos.ScrollToEnd();
        }

        private void EscribirMensajes()
        {
            richTxtResultados.AppendText(mensajes + "\n");
            richTxtResultados.ScrollToEnd();
        }

        private void BtnJugar_Click(object sender, RoutedEventArgs e)
        {
            sw.WriteLine("#JUGADA#" + txtJugada.Text + "#");
            sw.Flush();

            ////abrimos un fio para recibir os mensaxes que mande o servidor en bradcasting
            //Thread t = new Thread(EscucharResultado);
            //t.IsBackground = true;//pomos o fio como proceso en segundo plano
            //t.Start();
        }

        delegate void DelegadoMensajes();

        private void EscucharResultado()
        {
            int puer = System.Convert.ToInt32(puerto);
            //creamos un listenner no porto que lle mandamos ao server e ao que enviara os mensaxes
            TcpListener newSock = new TcpListener(IPAddress.Any, puer+1000);
            newSock.Start();
            Console.WriteLine("Esperando al servidor");

            //creamos un bucle infinito para que reciba constantemente as conexions do server 
            while (true)
            {
                TcpClient client = newSock.AcceptTcpClient();//linea bloqueante ata que non reciba un cliente
                NetworkStream ns = client.GetStream();
                StreamWriter sw = new StreamWriter(ns);
                StreamReader sr = new StreamReader(ns);

                if (sr.EndOfStream == true)
                {
                    Console.WriteLine("stream finalizado");
                }
                while (sr.Peek() > -1)
                {
                    mensajes += sr.ReadLine() + "\n";// liniea bloqueante ata que lle chegue algo do servidor
                }


                //chamamos ao metodo para escribir nun delegado por si acaso
                DelegadoMensajes dm = new DelegadoMensajes(EscribirMensajes);
                Dispatcher.Invoke(dm);

                mensajes = "";
            }
        }
    }
}
