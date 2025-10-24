using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Personas : DAL_Base<Personas>
    {
        public DAL_Personas() : base() { }
        public DAL_Personas(TransactionScope transaccion) : base(transaccion) { }

        protected override System.Data.Linq.Table<Personas> Tabla => bd.Personas;

        public override string ComboText => "ApellidosNombre";
        public override string ComboValue => "PER_Id";

        protected override Expression<Func<Personas, bool>> ObtenerPrefiltroPrimaryKey(object valorPK)
        {
            int valor = int.Parse(valorPK.ToString());
            return p => p.PER_Id == valor;
        }

        public override bool ConsultarMedianteClaveExterna => true;

        public enum TipoClaveExterna
        {
            AprobadoresEmpresa = 1
        }

        private TipoClaveExterna _modoConsultaClaveExterna;
        public TipoClaveExterna ModoConsultaClaveExterna
        {
            get => _modoConsultaClaveExterna;
            set => _modoConsultaClaveExterna = value;
        }

        public override List<Personas> L_ClaveExterna(object valorFK, Expression<Func<Personas, bool>> preFiltro = null)
        {
            switch (ModoConsultaClaveExterna)
            {
                case TipoClaveExterna.AprobadoresEmpresa:
                default:
                    return (from reg in bd.Empresas_Aprobadores
                            where reg.EMA_EMP_Id.Equals(valorFK)
                            select reg.Personas).ToList();
            }
        }

        protected override bool Guardar(Personas entidad, int idPersonaModificacion)
        {
            Personas item = L_PrimaryKey(entidad.PER_Id);
            bool esAlta = item == null;
            if (esAlta)
            {
                item = new Personas
                {
                    FechaAlta = DateTime.Now,
                    PER_Activo = true
                };
                bd.Personas.InsertOnSubmit(item);
            }

            // Campos editables
            item.PER_Nombre = entidad.PER_Nombre;
            item.PER_Apellido1 = entidad.PER_Apellido1;
            item.PER_Apellido2 = entidad.PER_Apellido2;
            item.PER_Email = entidad.PER_Email;
            item.PER_DEP_Id = entidad.PER_DEP_Id;
            item.PER_Activo = entidad.PER_Activo;

            item.FechaModificacion = DateTime.Now;
            item.PER_Id_Modificacion = idPersonaModificacion;

            bd.SubmitChanges();

            // devolver el nuevo PK en caso de alta
            entidad.PER_Id = item.PER_Id;
            return true;
        }

        /// <summary>
        /// En vez de borrar físicamente, marcamos como inactivo.
        /// </summary>
        public bool EliminarPersona(int id, int idPersonaModificacion)
        {
            var item = L_PrimaryKey(id);
            if (item == null) return false;
            item.PER_Activo = false;
            item.FechaModificacion = DateTime.Now;
            item.PER_Id_Modificacion = idPersonaModificacion;
            bd.SubmitChanges();
            return true;
        }

        public List<int> ObtenerSeccionesTrabajador(int pintPER_Id)
        {
            return (from per in bd.Personas
                    join sec in bd.Secciones on per.PER_DEP_Id equals sec.SEC_DEP_Id
                    where per.PER_Id.Equals(pintPER_Id)
                    select sec.SEC_Id).ToList();
        }
    }
}
