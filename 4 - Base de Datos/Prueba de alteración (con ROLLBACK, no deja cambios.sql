USE EdelstahlNegocio;
BEGIN TRAN;
    DECLARE @id uniqueidentifier = (SELECT TOP 1 Id FROM dbo.Clientes ORDER BY RazonSocial);
    UPDATE dbo.Clientes SET LimiteCredito = LimiteCredito + 1 WHERE Id = @id;
    -- Acá pegá la consulta 2: ese cliente tiene que salir como ALTERADO.
ROLLBACK TRAN;