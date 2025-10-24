using System;
using System.Linq;
using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Empresas_Departamentos : DAL_Base<Empresas_Departamentos>
    {
        public DAL_Empresas_Departamentos() : base() { }

        public DAL_Empresas_Departamentos(TransactionScope transaccion) : base(transaccion) { }

        protected override System.Data.Linq.Table<Empresas_Departamentos> Tabla
        {
            get { return bd.Empresas_Departamentos; }
        }

        protected override bool Guardar(Empresas_Departamentos obj, int idPersonaModificacion)
        {
            try
            {
                var existente = bd.Empresas_Departamentos
                                 .FirstOrDefault(d => d.EDE_Id == obj.EDE_Id);

                if (existente != null)
                {
                    existente.EDE_Nombre = obj.EDE_Nombre;
                }
                else
                {
                    bd.Empresas_Departamentos.InsertOnSubmit(obj);
                }

                bd.SubmitChanges();
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}