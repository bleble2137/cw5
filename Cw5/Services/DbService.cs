using Microsoft.Extensions.Configuration;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;


//Niestety niedokończone, bo zabrakło czasu

namespace Cw5
{
    public class DbService : IDbService

    {
        private readonly IConfiguration configuration;
        public DbService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public async Task<int> AddWarehouse(Warehouse warehouse)
        {
            warehouse.Amount = Math.Abs(warehouse.Amount);
            SqlConnection connection = GetNewConnection();

            using (SqlCommand com = new())
            {
                com.Connection = connection;
                await connection.OpenAsync();
                var transaction = await connection.BeginTransactionAsync();
                com.Transaction = transaction as SqlTransaction;
                try
                {
                    com.CommandText = $"SELECT * FROM Product WHERE IdProduct = @productId";
                    com.Parameters.AddWithValue("@productId", warehouse.IdProduct);
                    var rowsAffectedSelectProduct = com.ExecuteNonQuery();
                    if (rowsAffectedSelectProduct > 0)
                    {
                        throw new Exception("IdProduct taken");
                    }
                    com.Parameters.Clear();

                    com.CommandText = $"SELECT * FROM Warehouse WHERE IdWareHouse = @warehouseId";
                    com.Parameters.AddWithValue("@warehouseId", warehouse.IdWarehouse);
                    var rowsAffectedSelectWarehouse = com.ExecuteNonQuery();
                    if (rowsAffectedSelectWarehouse > 0)
                    {
                        throw new Exception("IdWareHouse taken");
                    }
                    if (warehouse.Amount < 1)
                    {
                        throw new Exception("Price lower than 1");
                    }
                    com.Parameters.Clear();


                    com.CommandText = $"SELECT * FROM [s20240].[dbo].[Order] WHERE IdProduct = @productId AND Amount = @amount";
                    com.Parameters.AddWithValue("@productId", warehouse.IdProduct);
                    com.Parameters.AddWithValue("@amount", warehouse.Amount);
                    var rowsAffectedSelectOrder = com.ExecuteNonQuery();
                    if (rowsAffectedSelectOrder == 0)
                    {
                        throw new Exception("No order, cannot add product");
                    }
                    com.Parameters.Clear();
                    com.CommandText = $"SELECT * FROM [s20240].[dbo].[Order] " +
                        $"Join Product_Warehouse ON [s20240].[dbo].[Order].IdProduct = Product_Warehouse.IdProduct " +
                        $"WHERE [s20240].[dbo].[Order].IdProduct = @productId " +
                        $"AND " +
                        $"[s20240].[dbo].[Order].CreatedAt <  @createdAt";
                    com.Parameters.AddWithValue("@productId", warehouse.IdProduct);
                    com.Parameters.AddWithValue("@createdAt", warehouse.CreatedAt);
                    var rowsAffectedSelectOrdersDate = com.ExecuteNonQuery();
                    if (rowsAffectedSelectOrdersDate == 0)
                    {
                        throw new Exception("Future date detected");
                    }
                    com.Parameters.Clear();


                    com.CommandText = $"SELECT * FROM Product_Warehouse WHERE IdOrder = @idOrder";
                    com.Parameters.AddWithValue("@idOrder", warehouse.IdOrder);
                    var rowsAffectedSelectOrdersId = com.ExecuteNonQuery();
                    if (rowsAffectedSelectOrdersId > 0)
                    {
                        throw new Exception("Error. Order already completed");
                    }
                    com.Parameters.Clear();

                    UpdateOrderDate(warehouse, com);

                    InsertNewProduct(warehouse, com);

                    var price = 0;
                    SqlDataReader dr = com.ExecuteReader();
                    while (dr.Read())
                    {
                        price = int.Parse(dr["Price"].ToString());
                    }

                    com.Parameters.Clear();
                    com.CommandText = $"INSERT INTO Product_Warehouse(Price) values(@price)";
                    com.Parameters.AddWithValue("@price", warehouse.Amount * price);

                    var primaryKey = int.Parse((await com.ExecuteScalarAsync()).ToString());
                    await transaction.CommitAsync();
                    return primaryKey;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception(ex.Message);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        private SqlConnection GetNewConnection()
        {
            return new SqlConnection(configuration.GetConnectionString("DefaultDbConnection"));
        }

        private static void UpdateOrderDate(Warehouse warehouse, SqlCommand com)
        {
            com.CommandText = $"UPDATE [s20240].[dbo].[Order] SET FulfilledAt = @date";
            com.Parameters.AddWithValue("@date", warehouse.CreatedAt);
            com.ExecuteNonQuery();
            com.Parameters.Clear();
        }

        private static void InsertNewProduct(Warehouse warehouse, SqlCommand com)
        {
            com.CommandText = $"INSERT INTO Product(IdProduct) values(Name, Description, Price) SELECT SCOPE_IDENTITY()";
            com.Parameters.AddWithValue("@idProduct", warehouse.IdProduct);
            com.ExecuteNonQuery();
            var a = com.ExecuteReader().Read();
            com.Parameters.Clear();

            com.CommandText = $"INSERT INTO Warehouse(IdWareHouse) values(@idWarehouse)";
            com.Parameters.AddWithValue("@idWarehouse", warehouse.IdWarehouse);
            com.ExecuteNonQuery();
            com.Parameters.Clear();

            com.CommandText = $"INSERT INTO Product_Warehouse(Amount) values(@amount)";
            com.Parameters.AddWithValue("@amount", warehouse.Amount);
            com.ExecuteNonQuery();
            com.Parameters.Clear();

            com.CommandText = $"INSERT INTO Product_Warehouse(CreatedAt) values(@created)";
            com.Parameters.AddWithValue("@created", DateTime.Now);
            com.ExecuteNonQuery();
            com.Parameters.Clear();

            com.CommandText = $"SELECT Price FROM Product WHERE IdProduct = @productId";
            com.Parameters.AddWithValue("@productId", warehouse.IdProduct);
        }
    }

}