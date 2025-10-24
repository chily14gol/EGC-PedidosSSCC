CREATE NONCLUSTERED INDEX IX_TLE_Anyo
ON dbo.Tareas_Empresas_LineasEsfuerzo (TLE_Anyo)
INCLUDE (TLE_Id, TLE_Mes, TLE_Descripcion, TLE_ESO_Id, FechaModificacion);

CREATE NONCLUSTERED INDEX IX_TLE_AnyoMes
ON dbo.Tareas_Empresas_LineasEsfuerzo (TLE_Anyo, TLE_Mes)
INCLUDE (TLE_Id, TLE_Descripcion, TLE_ESO_Id, FechaModificacion);

CREATE NONCLUSTERED INDEX IX_FechaModificacion
ON dbo.Tareas_Empresas_LineasEsfuerzo (FechaModificacion);

CREATE UNIQUE NONCLUSTERED INDEX IX_Entes_Email
ON Entes (ENT_Email ASC);