using System.Data;
using System.Globalization;
using System.Runtime.CompilerServices;
using Dapper;

namespace Fortino.Web.Data;

// SQLite salva 556 come intero e 91.80 come decimale nella stessa colonna NUMERIC.
// Dapper prepara la lettura sul tipo della prima riga e poi fallisce sulle altre:
// questo convertitore trasforma sempre il valore in decimal.
internal static class DapperSetup
{
    [ModuleInitializer]
    internal static void Configure()
    {
        SqlMapper.RemoveTypeMap(typeof(decimal));
        SqlMapper.RemoveTypeMap(typeof(decimal?));
        SqlMapper.AddTypeHandler(new DecimalHandler());
    }

    private sealed class DecimalHandler : SqlMapper.TypeHandler<decimal>
    {
        public override void SetValue(IDbDataParameter parameter, decimal value)
        {
            parameter.DbType = DbType.Decimal;
            parameter.Value = value;
        }

        public override decimal Parse(object value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }
}
