using System.Numerics;

namespace Conde.Envoltoria;

/// <summary>
/// O SORTEIO, escrito aqui em vez de vir da biblioteca.
///
/// A documentacao do sorteio de biblioteca nao promete a mesma sequencia entre
/// versoes nem entre sistemas, e a integracao continua roda em tres. Uma medida
/// de "o predicado com virgula errou em tantos casos" com pontos diferentes em
/// cada sistema nao e uma medida: sao tres medidas que ninguem pode comparar.
/// </summary>
public sealed class Sorteio
{
    private ulong estado;

    public Sorteio(ulong semente) => estado = semente == 0 ? 1 : semente;

    public uint Proximo()
    {
        estado = estado * 6364136223846793005UL + 1442695040888963407UL;
        return (uint)(estado >> 32);
    }

    public int Ate(int limite) => (int)(Proximo() % (uint)limite);

    public long Entre(long menor, long maior) =>
        menor + (long)(((ulong)Proximo() << 32 | Proximo()) % (ulong)(maior - menor + 1));

    public double Fracao() => Proximo() / 4294967296.0;
}

/// <summary>
/// Um PONTO de coordenadas INTEIRAS.
///
/// Inteiras de proposito, e o repositorio inteiro existe para explicar por que. A
/// geometria computacional e cheia de decisoes do tipo "este ponto esta de que
/// lado desta reta", e essas decisoes nao toleram aproximacao: uma resposta errada
/// nao da um resultado um pouco torto, ela da um resultado que nao e um poligono.
///
/// Com coordenadas inteiras, o predicado de orientacao e um determinante de
/// inteiros, e ele e exato. Com coordenadas com virgula, ele e uma subtracao de
/// dois produtos quase iguais, que e o caso em que o arredondamento mais machuca.
/// </summary>
public readonly record struct Ponto(long X, long Y)
{
    public override string ToString() => $"({X}, {Y})";

    public static Ponto operator -(Ponto a, Ponto b) => new(a.X - b.X, a.Y - b.Y);

    /// <summary>O quadrado da distancia, exato: sem raiz, sem virgula.</summary>
    public BigInteger DistanciaAoQuadrado(Ponto outro)
    {
        var dx = (BigInteger)X - outro.X;
        var dy = (BigInteger)Y - outro.Y;

        return dx * dx + dy * dy;
    }
}

/// <summary>
/// Os CONJUNTOS DE PONTOS que separam os algoritmos, e por que eles sao assim.
///
/// Pontos sorteados num quadrado nao separam nada: a envoltoria tem umas poucas
/// dezenas de vertices e todo algoritmo vai bem. O que separa sao os casos
/// DEGENERADOS, e eles sao o caso comum em dado de verdade: coordenadas vindas de
/// uma grade, pontos repetidos, tres pontos em linha reta.
/// </summary>
public static class Conjuntos
{
    /// <summary>Pontos sorteados num quadrado: o caso facil.</summary>
    public static List<Ponto> Sorteados(int quantos, long lado = 1_000_000, ulong semente = 7)
    {
        var sorteio = new Sorteio(semente);

        return [.. Enumerable.Range(0, quantos).Select(_ => new Ponto(sorteio.Entre(0, lado), sorteio.Entre(0, lado)))];
    }

    /// <summary>
    /// Pontos sorteados numa GRADE pequena: muitos deles ficam em linha reta.
    ///
    /// E o caso de verdade: coordenada de tela, coordenada arredondada para metro,
    /// posicao num tabuleiro. Tres pontos em linha reta sao a fonte de metade dos
    /// bugs de envoltoria convexa.
    /// </summary>
    public static List<Ponto> NaGrade(int quantos, long lado = 30, ulong semente = 11)
    {
        var sorteio = new Sorteio(semente);

        return [.. Enumerable.Range(0, quantos).Select(_ => new Ponto(sorteio.Entre(0, lado), sorteio.Entre(0, lado)))];
    }

    /// <summary>
    /// TODOS os pontos na borda de um circulo: a envoltoria e o conjunto inteiro.
    ///
    /// E o pior caso do embrulho de presente, que custa o numero de pontos vezes o
    /// numero de vertices da envoltoria.
    /// </summary>
    public static List<Ponto> NoCirculo(int quantos, long raio = 1_000_000)
    {
        var pontos = new List<Ponto>();

        for (var i = 0; i < quantos; i++)
        {
            var angulo = 2 * Math.PI * i / quantos;

            pontos.Add(new Ponto((long)(raio * Math.Cos(angulo)), (long)(raio * Math.Sin(angulo))));
        }

        return [.. pontos.Distinct()];
    }

    /// <summary>
    /// Pontos TODOS EM LINHA RETA: a envoltoria degenera.
    ///
    /// Com todos colineares nao ha poligono nenhum, e o que cada algoritmo devolve
    /// nesse caso e uma escolha de projeto que quase nunca esta documentada.
    /// </summary>
    public static List<Ponto> EmLinhaReta(int quantos, long passo = 7) =>
        [.. Enumerable.Range(0, quantos).Select(i => new Ponto(i * passo, i * passo * 3))];

    /// <summary>
    /// Pontos COM REPETIDOS, que todo dado de verdade tem.
    /// </summary>
    public static List<Ponto> ComRepetidos(int quantos, ulong semente = 13)
    {
        var sorteio = new Sorteio(semente);
        var base_ = Sorteados(Math.Max(1, quantos / 4), 1000, semente);

        return [.. Enumerable.Range(0, quantos).Select(_ => base_[sorteio.Ate(base_.Count)])];
    }

    /// <summary>
    /// Pontos QUASE COLINEARES, com coordenadas grandes: o caso em que o predicado
    /// com virgula desmonta.
    ///
    /// A ESCALA e o que importa aqui, e descobrir isso me custou uma tabela inteira
    /// de zeros. Com coordenadas de ate dez milhoes, os dois produtos do
    /// determinante ficam na ordem de 10^14, que e MENOR que 2^53: a precisao dupla
    /// representa os dois EXATAMENTE, a subtracao de dois inteiros exatos tambem e
    /// exata, e o predicado com virgula acerta tudo.
    ///
    /// So acima de 2^53, que e nove mil trilhoes, o arredondamento comeca. Com
    /// coordenadas de ate um bilhao, os produtos passam de 10^18 e ai sim os dois
    /// perdem digitos antes de a subtracao acontecer.
    /// </summary>
    public static List<Ponto> QuaseEmLinha(int quantos, ulong semente = 17, long escala = 1_000_000_000)
    {
        var sorteio = new Sorteio(semente);
        var pontos = new List<Ponto>();

        for (var i = 0; i < quantos; i++)
        {
            var x = sorteio.Entre(0, escala);
            var desvio = sorteio.Entre(-1, 1);

            pontos.Add(new Ponto(x, 3 * x + desvio));
        }

        return pontos;
    }

    /// <summary>
    /// Os pontos de FIBONACCI: (F(i), F(i+1)), que e o conjunto que DERRUBA o
    /// predicado com virgula.
    ///
    /// Achar um conjunto que derrube o predicado nao e tao facil quanto parece, e
    /// as minhas quatro primeiras tentativas deram uma tabela inteira de zeros. Com
    /// coordenadas de ate um bilhao e pontos "quase em linha", o determinante ainda
    /// fica na ordem de 10^9 e o erro de arredondamento na de 10^2: o sinal continua
    /// certo.
    ///
    /// O que derruba e o determinante ficar MENOR que o erro. A razao entre dois
    /// numeros de Fibonacci seguidos e a melhor aproximacao racional possivel da
    /// razao aurea, entao os pontos (F(i), F(i+1)) ficam colados numa reta de um
    /// jeito que nenhuma outra escolha de inteiros consegue: o determinante de tres
    /// deles sai na casa das unidades, com os produtos na casa de 10^18.
    ///
    /// E ai os dois produtos perdem os digitos de baixo antes da subtracao, e o
    /// sinal vira sorteio.
    /// </summary>
    public static List<Ponto> Fibonacci(int quantos = 25, int comecando = 20)
    {
        var fib = new List<long> { 0, 1 };
        while (fib.Count < comecando + quantos + 2) fib.Add(fib[^1] + fib[^2]);

        return [.. Enumerable.Range(comecando, quantos).Select(i => new Ponto(fib[i], fib[i + 1]))];
    }

    public static IEnumerable<(string Nome, List<Ponto> Pontos)> Todos(int quantos)
    {
        yield return ("sorteados", Sorteados(quantos));
        yield return ("na grade", NaGrade(quantos));
        yield return ("no circulo", NoCirculo(quantos));
        yield return ("com repetidos", ComRepetidos(quantos));
        yield return ("quase em linha", QuaseEmLinha(quantos));
        yield return ("coordenadas grandes", Sorteados(quantos, 2_000_000_000, 23));
        yield return ("Fibonacci", Fibonacci(Math.Min(quantos, 60)));
    }
}
