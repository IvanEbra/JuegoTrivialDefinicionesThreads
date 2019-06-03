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

namespace AdivinaPalabra
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<PalabraOculta> listaPalabras = new List<PalabraOculta>();
        List<int> listaIndices = new List<int>();

        public MainWindow()
        {
            InitializeComponent();
            CargarPalabras();
        }

        private void CargarPalabras()
        {
            String linea;
            StreamReader readerDiccionario = new StreamReader("diccionario.txt",System.Text.Encoding.Default);
            while (readerDiccionario.Peek() > -1)
            {
                linea = readerDiccionario.ReadLine();
                String[] dividida=linea.Split('.');
                String palabra = dividida[0].ToLower();
                String definicion = dividida[1];
                listaPalabras.Add(new PalabraOculta(definicion, palabra));
            }
        }

        //variable para identificar o xogador 1
        String jugador1 = "";
        String idJugador1 = ""; //referencia IP:port
        String ipJugador1 = "";
        String jugadaJugador1 = "";
        String puertoJugador1 = "";//puerto polo que vai falar o xogador
        int puntosJugador1 = 0;

        //variable para identificar o xogador 2
        String jugador2 = "";
        String idJugador2 = ""; //referencia IP:port
        String ipJugador2 = "";
        String jugadaJugador2 = "";
        String puertoJugador2 = "";//puerto polo que vai falar o xogador
        int puntosJugador2 = 0;

        //variables do xogo
        int ronda = 1;
        PalabraOculta palabraOculta;
        String acierta = "";



        private void BtnConectarServidor_Click(object sender, RoutedEventArgs e)
        {
            //creamos un thread para que reciba clientes continuamente asi non se queda colgada a ventana de conectar server
            Thread t = new Thread(this.EsperarClientes);
            t.Start();
            btnConectarServidor.IsEnabled = false;
        }

        private void EsperarClientes()
        {
            //preparamos o escoitador que vai mirar as peticions de conexion nese porto
            TcpListener newSock = new TcpListener(IPAddress.Any, 2000);
            newSock.Start();

            Console.WriteLine("esperando cliente");

            while (true)
            {
                //linea bloqueante que acepta os clientes e abre un fio para cada un

                TcpClient cliente = newSock.AcceptTcpClient();
                Thread t = new Thread(() => ManejarCliente(cliente));
                t.Start();
            }
        }

        private void ManejarCliente(TcpClient cliente)
        {
            //canal de comunicacion cos clientes
            NetworkStream ns = cliente.GetStream();
            //leer e escribir nese canal
            StreamReader sr = new StreamReader(ns);
            StreamWriter sw = new StreamWriter(ns);
            //recollida dos datos recibidos
            String dato;

            //mensaxe de benvida e informe de protocolo
            sw.WriteLine("#INSCRIBIR#nick#puertoEscucha#");
            sw.WriteLine("#JUGADA#intentoPalabra#");
            sw.WriteLine("------------------------------");
            sw.Flush();


            //preparamonos para escoitar o que nos mande o cliente continuamente
            while (true)
            {
                try
                {
                    //linea bloqueante que espera a que o cliente mande algo
                    dato = sr.ReadLine();
                    String[] subdatos = dato.Split('#');

                    //miramos que peticion nos mandou ocliente subdatos[1]
                    #region conINSCRIBIR
                    if (subdatos[1].Equals("INSCRIBIR"))
                    {
                        //se inda non hai ningun xogador o que entre sera o 1
                        if (jugador1.Equals(""))
                        {
                            jugador1 = subdatos[2];
                            idJugador1 = cliente.Client.RemoteEndPoint.ToString();
                            ipJugador1 = idJugador1.Split(':')[0];
                            puertoJugador1 = subdatos[3];
                            sw.WriteLine("#OK#");
                            sw.WriteLine("------------------------------");
                            sw.Flush();
                        }
                        //se hai o xogador1 metemos o 2
                        else if (jugador2.Equals(""))
                        {
                            Console.WriteLine("INSCRIBIENDO JUGADOR 2");
                            jugador2 = subdatos[2];
                            idJugador2 = cliente.Client.RemoteEndPoint.ToString();
                            ipJugador2 = idJugador2.Split(':')[0];
                            puertoJugador2 = subdatos[3];
                            sw.WriteLine("#OK#");
                            sw.WriteLine("------------------------------");
                            sw.Flush();
                            ComienzaElJuego();
                            Thread empezarRondas = new Thread(EmpezarRondas);
                            empezarRondas.IsBackground = true;
                            empezarRondas.Start();
                            Console.WriteLine("FIN INSCRIBIENDO JUGADOR 2");
                        }
                        //se estan todos os xogadores completos
                        else
                        {
                            sw.WriteLine("#NOK#ya hay dos jugadores#");
                            sw.WriteLine("------------------------------");
                            sw.Flush();
                        }
                    }
                    #endregion

                    #region conJUGADA
                    if (subdatos[1].Equals("JUGADA"))
                    {
                        //miramos quen fai a xogada
                        if (cliente.Client.RemoteEndPoint.ToString().Equals(idJugador1))//estamos co xogador 1
                        {
                            jugadaJugador1 = subdatos[2].ToLower();
                            Thread comprobarJugadaJugador1 = new Thread(() => ComprobarPalabra(cliente));
                            comprobarJugadaJugador1.Start();
                        }
                        else if (cliente.Client.RemoteEndPoint.ToString().Equals(idJugador2))
                        {
                            Console.WriteLine("JUGADA JUGADOR 2");
                            jugadaJugador2 = subdatos[2].ToLower();
                            Thread comprobarJugadaJugador2 = new Thread(() => ComprobarPalabra(cliente));
                            comprobarJugadaJugador2.Start();
                        }
                    }
                    #endregion
                }
                catch (Exception er)
                {
                    Console.WriteLine("ERROR: " + er.ToString());
                }
            }
        }

        private void ComienzaElJuego()
        {
            //variables para comunicarse cos clientes
            TcpClient cliente;
            NetworkStream ns;
            StreamReader sr;
            StreamWriter sw;

            //mandamosllo ao primer xogador
            int ptoJugador1 = System.Convert.ToInt32(puertoJugador1);
            cliente = new TcpClient(ipJugador1, ptoJugador1);
            ns = cliente.GetStream();
            sr = new StreamReader(ns);
            sw = new StreamWriter(ns);
            sw.WriteLine("¡QUE EMPIECE EL JUEGO!");
            sw.WriteLine("----------------------");
            sw.Flush();

            int ptoJugador2 = System.Convert.ToInt32(puertoJugador2);
            //mandamosllo ao segundo xogador
            cliente = new TcpClient(ipJugador2, ptoJugador2);
            ns = cliente.GetStream();
            sr = new StreamReader(ns);
            sw = new StreamWriter(ns);
            sw.WriteLine("¡QUE EMPIECE EL JUEGO!");
            sw.WriteLine("----------------------");
            sw.Flush();

           
        }

        int contador = 60;//1 minuto por palabra

        private void EmpezarRondas()
        {
            for (int i = 0; i < 3; i++)//cada iteracion do bucle e unha ronda
            {
                palabraOculta = SacarPalabra();
                //mostramoslle a palabra a cada xogador
                MostrarPalabra(palabraOculta);
                //abrimos un fio para cada palabra e facemos que espere ata que se acabe esa palabra cun thread.join. E O QUE CAMBIA AS OCULTAS POR LETRAS??
                Thread activarTimer = new Thread(RestarTimer);
                activarTimer.Start();
                activarTimer.Join();
                activarTimer.Abort();
                contador = 60;
            }

            //variables para comunicarse cos clientes
            TcpClient cliente;
            NetworkStream ns;
            StreamReader sr;
            StreamWriter sw;

            //mandamosllo ao primer xogador
            int ptoJugador1 = System.Convert.ToInt32(puertoJugador1);
            cliente = new TcpClient(ipJugador1, ptoJugador1+1000);
            ns = cliente.GetStream();
            sr = new StreamReader(ns);
            sw = new StreamWriter(ns);
            sw.WriteLine("PUNTUACION: " + jugador1 + puntosJugador1);
            sw.WriteLine("PUNTUACION: " + jugador2 + puntosJugador2);
            sw.Flush();

            int ptoJugador2 = System.Convert.ToInt32(puertoJugador2);
            //mandamosllo ao segundo xogador
            cliente = new TcpClient(ipJugador2, ptoJugador2 + 1000);
            ns = cliente.GetStream();
            sr = new StreamReader(ns);
            sw = new StreamWriter(ns);
            sw.WriteLine("PUNTUACION: " + jugador2 + "=" + puntosJugador2);
            sw.WriteLine("PUNTUACION: " + jugador1 + "=" + puntosJugador1);
            sw.Flush();

        }

        private PalabraOculta SacarPalabra()
        {
            Random rnd = new Random();
            int posicionPalabra;

            do
            {
                posicionPalabra = rnd.Next(0, listaPalabras.Count);
                if (listaIndices.Contains(posicionPalabra))
                {
                    posicionPalabra = -1;
                    Console.WriteLine(-1);
                }
            } while (posicionPalabra==-1);

            return listaPalabras[posicionPalabra];
        }

        private void MostrarPalabra(PalabraOculta palabraOculta)
        {
            //variables para comunicarse cos clientes
            TcpClient cliente;
            NetworkStream ns;
            StreamReader sr;
            StreamWriter sw;

            //mandamosllo ao primer xogador
            int ptoJugador1 = System.Convert.ToInt32(puertoJugador1);
            cliente = new TcpClient(ipJugador1, ptoJugador1);
            ns = cliente.GetStream();
            sr = new StreamReader(ns);
            sw = new StreamWriter(ns);
            sw.WriteLine(palabraOculta.definicion);
            sw.WriteLine(palabraOculta.oculta);
            sw.WriteLine("----------------------");
            sw.Flush();

            int ptoJugador2 = System.Convert.ToInt32(puertoJugador2);
            //mandamosllo ao segundo xogador
            cliente = new TcpClient(ipJugador2, ptoJugador2);
            ns = cliente.GetStream();
            sr = new StreamReader(ns);
            sw = new StreamWriter(ns);
            sw.WriteLine(palabraOculta.definicion);
            sw.WriteLine(palabraOculta.oculta);
            sw.WriteLine("----------------------");
            sw.Flush();
        }

        private void RestarTimer()
        {
            while (contador > 0)
            {
                contador--;
                Thread.Sleep(1000);
            }
        }




        private void ComprobarPalabra(TcpClient cliente)
        {
            Console.WriteLine("COMPROBANDO PALABRA");
            if (cliente.Client.RemoteEndPoint.ToString().Equals(idJugador1))
            {
                Console.WriteLine("COMPROBANDO PALABRA DE JUGADOR 1");
                if (jugadaJugador1.Equals(palabraOculta.palabra))
                {
                    Console.WriteLine("JUGADOR 1 HA HACERTADO, CAMBIANDO ACIERTA");
                    //comunicamos a todos que acertou
                    CambiarAcierta(cliente);
                    ComunicarGanador();
                    acierta = "";
                    contador = 0;
                }
                else
                {
                    Console.WriteLine("PALABRA ERRONEA");
                    //mandamoslle solo a este cliente que fallou
                    ComunicarFallo(cliente);
                }
            }
            if (cliente.Client.RemoteEndPoint.ToString().Equals(idJugador2))
            {
                Console.WriteLine("COMPROBANDO PALABRA DE JUGADOR 2");
                if (jugadaJugador2.Equals(palabraOculta.palabra))
                {
                    Console.WriteLine("JUGADOR 2 HA HACERTADO, CAMBIANDO ACIERTA");
                    //comunicamos a todos que acertou
                    CambiarAcierta(cliente);
                    ComunicarGanador();
                    acierta = "";
                    contador = 0;
                }
                else
                {
                    Console.WriteLine("PALABRA ERRONEA");
                    //mandamoslle solo a este cliente que fallou
                    ComunicarFallo(cliente);
                }
            }
        }

        private void ComunicarFallo(TcpClient cliente)
        {
            if (cliente.Client.RemoteEndPoint.ToString().Equals(idJugador1))
            {
                Console.WriteLine("COMUNICANDO FALLO JUGADOR1");
                int puerto1 = Convert.ToInt32(puertoJugador1);
                TcpClient cli1 = new TcpClient(ipJugador1, puerto1 + 1000);
                NetworkStream ns=cli1.GetStream();
                StreamWriter sw=new StreamWriter(ns);
                StreamReader sr=new StreamReader(ns);
                sw.WriteLine("ERROR, prueba otra vez");
                sw.Flush();
            }
            if (cliente.Client.RemoteEndPoint.ToString().Equals(idJugador2))
            {
                int puerto2 = Convert.ToInt32(puertoJugador2);
                TcpClient cli2 = new TcpClient(ipJugador1, puerto2 + 1000);
                NetworkStream ns = cli2.GetStream();
                StreamWriter sw = new StreamWriter(ns);
                StreamReader sr = new StreamReader(ns);
                sw.WriteLine("ERROR, prueba otra vez");
                sw.Flush();
            }
        }


        private void ComunicarGanador()
        {
            Console.WriteLine("COMUNICANDO GANADOR");
            //variables para comunicarse cos clientes
            TcpClient cliente;
            NetworkStream ns;
            StreamReader sr;
            StreamWriter sw;
            if (acierta.Equals(jugador1))
            {
                Console.WriteLine("COMUNICANDO GANADOR JUGADOR 1");
                //mandamosllo ao primer xogador
                int ptoJugador1 = System.Convert.ToInt32(puertoJugador1);
                cliente = new TcpClient(ipJugador1, ptoJugador1+1000);
                ns = cliente.GetStream();
                sr = new StreamReader(ns);
                sw = new StreamWriter(ns);
                sw.WriteLine("HAS ACERTADO");
                sw.WriteLine("----------------------");
                sw.Flush();

                int ptoJugador2 = System.Convert.ToInt32(puertoJugador2);
                //mandamosllo ao segundo xogador
                cliente = new TcpClient(ipJugador2, ptoJugador2+1000);
                ns = cliente.GetStream();
                sr = new StreamReader(ns);
                sw = new StreamWriter(ns);
                sw.WriteLine("HA ACERTADO EL JUGADOR 1");
                sw.WriteLine("----------------------");
                sw.Flush();
            }
            if (acierta.Equals(jugador2))
            {
                Console.WriteLine("COMUNICANDO GANADOR JUGADOR 2");
                //mandamosllo ao primer xogador
                int ptoJugador1 = System.Convert.ToInt32(puertoJugador1);
                cliente = new TcpClient(ipJugador1, ptoJugador1 + 1000);
                ns = cliente.GetStream();
                sr = new StreamReader(ns);
                sw = new StreamWriter(ns);
                sw.WriteLine("HA ACERTADO EL JUGADOR 2");
                sw.WriteLine("----------------------");
                sw.Flush();

                int ptoJugador2 = System.Convert.ToInt32(puertoJugador2);
                //mandamosllo ao segundo xogador
                cliente = new TcpClient(ipJugador2, ptoJugador2 + 1000);
                ns = cliente.GetStream();
                sr = new StreamReader(ns);
                sw = new StreamWriter(ns);
                sw.WriteLine("HAS ACERTADO");
                sw.WriteLine("----------------------");
                sw.Flush();
            }
        }

        object o=new object();

        private void CambiarAcierta(TcpClient cliente)
        {
            Console.WriteLine("CAMBIANDO ACIERTA");
            lock (o)
            {
                if (String.IsNullOrEmpty(acierta))
                {
                    if (cliente.Client.RemoteEndPoint.ToString().Equals(idJugador1))
                    {
                        if (jugadaJugador1.Equals(palabraOculta.palabra))
                        {
                            Console.WriteLine("CAMBIANDO ACIERTA PARA JUGADOR 1");
                            puntosJugador1++;
                            acierta = jugador1;
                        }
                    }
                    if (cliente.Client.RemoteEndPoint.ToString().Equals(idJugador2))
                    {
                        if (jugadaJugador2.Equals(palabraOculta.palabra))
                        {
                            Console.WriteLine("CAMBIANDO ACIERTA PARA JUGADOR 2");
                            puntosJugador2++;
                            acierta = jugador2;
                        }
                    }
                }
            }
        }

    }
}
