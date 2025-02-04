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
    
    public partial class Cita
    {
        public int idCita { get; set; }
        public string TipoPago { get; set; }
        public Nullable<System.DateTime> FechaReferencia { get; set; }
        public string NoOrden { get; set; }
        public string EstatusPago { get; set; }
        public string TipoLicencia { get; set; }
        public string NoExpediente { get; set; }
        public Nullable<System.DateTime> FechaCita { get; set; }
        public string Sucursal { get; set; }
        public string Doctor { get; set; }
        public string Recepcionista { get; set; }
        public Nullable<int> idCanal { get; set; }
        public string Folio { get; set; }
        public Nullable<int> idPaciente { get; set; }
        public string TipoTramite { get; set; }
        public string Referencia { get; set; }
        public string Canal { get; set; }
        public string CC { get; set; }
        public string Asistencia { get; set; }
        public string CancelaComentario { get; set; }
        public string Entregado { get; set; }
        public string ReferidoPor { get; set; }
        public string CarruselMedico { get; set; }
        public Nullable<System.DateTime> FechaCreacion { get; set; }
        public string Cuenta { get; set; }
        public string CanalTipo { get; set; }
        public string CuentaComentario { get; set; }
        public string Conciliado { get; set; }
        public Nullable<System.DateTime> FechaContable { get; set; }
        public string UsarSaldo { get; set; }
        public string FormaPago { get; set; }
        public string PrecioEpi { get; set; }
        public string ConciliarPago { get; set; }
        public string RastreoPagos { get; set; }
        public string Venta { get; set; }
        public string IVA { get; set; }
        public string CostoSeguro { get; set; }
        public string IvaSeguro { get; set; }
        public string TotalSeguro { get; set; }
        public string TotalVenta { get; set; }
        public string ventaSeguro { get; set; }
        public string TotalIVA { get; set; }
        public string TotalVentaIVA { get; set; }
    
        public virtual Canal Canal1 { get; set; }
        public virtual Paciente Paciente { get; set; }
    }
}
