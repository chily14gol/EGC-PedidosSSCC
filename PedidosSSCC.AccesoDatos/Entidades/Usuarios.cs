using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;

namespace AccesoDatos
{
    partial class Usuarios : Entidad_Base
	{
        #region Miembros de Entidad_Base

        public override object ValorPK
        {
            get { return this.USU_Id; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.General; } }

        private string usu_Id_Ant = string.Empty;

        #endregion

		public string NombrePersona
		{
			get
			{
				if (this.PersonaUsuario == null) return null;
				return this.PersonaUsuario.ApellidosNombre;
			}
		}

        public Dictionary<string, bool> OpcionesAcceso
        {
            get
            {
                Dictionary<string, bool> dicOpciones = new Dictionary<string, bool>();
                if (this.USU_SPE_Id.HasValue)
                {
                    foreach (Seguridad_Perfiles_Opciones opcion in this.Seguridad_Perfiles.Seguridad_Perfiles_Opciones)
                    {
                        dicOpciones.Add(opcion.SPO_SOP_Id, opcion.SPO_Escritura);
                    }
                }
                
                return dicOpciones;
            }
        }


        private void EliminarPermiso(ref Dictionary<string,bool> dicOpciones, string pstrOpcion)
        {
            if (dicOpciones.ContainsKey(pstrOpcion))
                dicOpciones.Remove(pstrOpcion);
        }
                
        public string USU_Id_Ant
        {
            get
            {
                if (this.usu_Id_Ant.IsNullOrEmpty()) return USU_Id;
                return this.usu_Id_Ant;
            }
            set
            {
                usu_Id_Ant = value;
            }
        }

        private string _PER_Email_Actualizar = string.Empty;
        public string PER_Email_Actualizar
        {
            get
            {
                return this._PER_Email_Actualizar;
            }
            set
            {
                _PER_Email_Actualizar = value;
            }
        }
    }
}
