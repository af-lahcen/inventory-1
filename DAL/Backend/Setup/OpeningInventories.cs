using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Frapid.Configuration;
using Frapid.Framework.Extensions;
using MixERP.Inventory.ViewModels;
using Npgsql;

namespace MixERP.Inventory.DAL.Backend.Setup
{
    public static class OpeningInventories
    {
        public static async Task<long> PostAsync(string tenant, OpeningInventory model)
        {
            string connectionString = FrapidDbServer.GetConnectionString(tenant);

            string sql = @"SELECT * FROM inventory.post_opening_inventory
                            (
                                @OfficeId, @UserId, @LoginId, 
                                @ValueDate, @BookDate, 
                                @ReferenceNumber, @StatementReference, 
                                ARRAY[{0}]
                            );";

            sql = string.Format(sql, GetParametersForDetails(model.Details));

            using (var connection = new NpgsqlConnection(connectionString))
            {
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@OfficeId", model.OfficeId);
                    command.Parameters.AddWithValue("@UserId", model.UserId);
                    command.Parameters.AddWithValue("@LoginId", model.LoginId);
                    command.Parameters.AddWithValue("@ValueDate", model.ValueDate);
                    command.Parameters.AddWithValue("@BookDate", model.BookDate);
                    command.Parameters.AddWithValue("@ReferenceNumber", model.ReferenceNumber);
                    command.Parameters.AddWithValue("@StatementReference", model.StatementReference);

                    command.Parameters.AddRange(AddParametersForDetails(model.Details).ToArray());

                    connection.Open();
                    var awaiter = await command.ExecuteScalarAsync().ConfigureAwait(false);
                    return awaiter.To<long>();
                }
            }
        }

        public static string GetParametersForDetails(List<OpeningStockType> details)
        {
            if (details == null)
            {
                return "NULL::inventory.opening_stock_type";
            }

            var items = new Collection<string>();
            for (int i = 0; i < details.Count; i++)
            {
                items.Add(string.Format(CultureInfo.InvariantCulture,
                    "ROW(@StoreId{0}, @ItemId{0}, @Quantity{0}, @UnitId{0},@Price{0})::inventory.opening_stock_type",
                    i.ToString(CultureInfo.InvariantCulture)));
            }

            return string.Join(",", items);
        }

        public static IEnumerable<NpgsqlParameter> AddParametersForDetails(List<OpeningStockType> details)
        {
            var parameters = new List<NpgsqlParameter>();

            if (details != null)
            {
                for (int i = 0; i < details.Count; i++)
                {
                    parameters.Add(new NpgsqlParameter("@StoreId" + i, details[i].StoreId));
                    parameters.Add(new NpgsqlParameter("@ItemId" + i, details[i].ItemId));
                    parameters.Add(new NpgsqlParameter("@Quantity" + i, details[i].Quantity));
                    parameters.Add(new NpgsqlParameter("@UnitId" + i, details[i].UnitId));
                    parameters.Add(new NpgsqlParameter("@Price" + i, details[i].Price));
                }
            }

            return parameters;
        }
    }
}