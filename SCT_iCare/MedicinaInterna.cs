//------------------------------------------------------------------------------
// <auto-generated>
//     Este código se generó a partir de una plantilla.
//
//     Los cambios manuales en este archivo pueden causar un comportamiento inesperado de la aplicación.
//     Los cambios manuales en este archivo se sobrescribirán si se regenera el código.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SCT_iCare
{
    using System;
    using System.Collections.Generic;
    
    public partial class MedicinaInterna
    {
        public int idMedicinaInterna { get; set; }
        public Nullable<int> idPacienteDS { get; set; }
        public string CertificadoMedico { get; set; }
        public string PlanTratamiento { get; set; }
        public string Medicamento { get; set; }
        public Nullable<System.DateTime> Seguimiento { get; set; }
    
        public virtual PacienteDS PacienteDS { get; set; }
    }
}
