using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify.Modelos
{
    public class Plan
    {
        public int Id { get; set; }
        public string Nombre { get; set; } // Nombre del plan (e.g., "Premium", "Free")
        public double PrecioMensual { get; set; } // Precio mensual del plan
        public int MaximoUsuarios { get; set; } // Número de dispositivos permitidos para el plan
        public string Descripcion { get; set; } // Descripción del plan
    }
}
