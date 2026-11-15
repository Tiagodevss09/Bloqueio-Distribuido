namespace Bloqueio.Distribuido.Domain.Entities
{

    public sealed class Conta
    {
        public int Numero { get; private set; }

        public static decimal Saldo { get; private set; } = 1_000m;

        public decimal ValorDisponivel()
        {
            return Saldo;
        }

        public bool Sacar(decimal valor)
        {
            if (valor <= 0)
            {
                throw new ArgumentException("O valor do saque deve ser maior que zero.", nameof(valor));
            }
            Saldo -= valor;
            return true;
        }
    }

}

