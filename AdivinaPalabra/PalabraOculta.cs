using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdivinaPalabra
{
    public class PalabraOculta
    {
        public String definicion { get; set; }
        public String palabra { get; set; }
        public String oculta { get; set; }

        public PalabraOculta(String definicion, String palabra)
        {
            this.definicion = definicion;
            this.palabra = palabra;
            this.oculta = OcultarPalabra(palabra);
        }

        private String OcultarPalabra(String palabra)
        {
            String oc = "";

            for (int i = 0; i < palabra.Length; i++)
            {
                oc += "_ ";
            }

            return oc;
        }
    }
}
