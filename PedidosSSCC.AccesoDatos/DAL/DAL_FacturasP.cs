using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;

namespace AccesoDatos
{
    public class DAL_FacturasP : DAL_Base<FacturasP>
    {
        public DAL_FacturasP() : base() { }
        public DAL_FacturasP(TransactionScope transaccion) : base(transaccion) { }

        protected override System.Data.Linq.Table<FacturasP> Tabla
        {
            get { return bd.FacturasP; }
        }

        protected override bool Guardar(FacturasP entidad, int idPersonaModificacion)
        {
            bool retorno = false;

            FacturasP item = L_PrimaryKey(entidad.FAP_Id);

            if (item == null)
            {
                item = new FacturasP();
                item.FechaAlta = DateTime.Now;
                bd.FacturasP.InsertOnSubmit(item);
            }

            bd.SubmitChanges();
            entidad.FAP_Id = item.FAP_Id; //Asignamos el ID autonumerico al objeto "origen" para devolverlo en el caso de un alta.try

            retorno = true;

            return retorno;
        }
    }
}

