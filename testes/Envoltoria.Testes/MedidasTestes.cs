using Conde.Envoltoria;
using Xunit;

namespace Conde.Envoltoria.Testes;

/// <summary>
/// Os testes das TABELAS que o medidor imprime.
///
/// Eles existem porque numero em README que ninguem confere envelhece errado em
/// silencio: alguem troca uma constante, a tabela muda e o texto continua dizendo
/// a frase antiga. Aqui o teste afirma sobre o mesmo objeto que vira linha
/// impressa.
/// </summary>
public class MedidasTestes
{
    /// <summary>
    /// A linha principal: com o predicado exato, os tres algoritmos batem com a
    /// definicao em TODOS os conjuntos.
    /// </summary>
    [Fact]
    public void ComOPredicadoExatoTudoBate()
    {
        var linhas = Medidas.Juiz(120);

        Assert.Equal(21, linhas.Count);
        Assert.All(linhas, l => Assert.True(l.Convexa, $"{l.Conjunto}/{l.Algoritmo}"));
        Assert.All(linhas, l => Assert.True(l.Contem, $"{l.Conjunto}/{l.Algoritmo}"));
        Assert.All(linhas, l => Assert.True(l.Bate, $"{l.Conjunto}/{l.Algoritmo}"));
    }

    /// <summary>
    /// E os tres dao o MESMO numero de vertices em cada conjunto.
    /// </summary>
    [Fact]
    public void OsTresDaoOMesmoNumeroDeVertices()
    {
        foreach (var grupo in Medidas.Juiz(120).GroupBy(l => l.Conjunto))
            Assert.Single(grupo.Select(l => l.Vertices).Distinct());
    }

    /// <summary>
    /// A frase do texto: o predicado com virgula so erra nos pontos de Fibonacci, e
    /// la ele erra na maioria dos trios e se contradiz num terco deles.
    /// </summary>
    [Fact]
    public void OPredicadoComVirgulaSoErraEmFibonacci()
    {
        var linhas = Medidas.Predicados(60);

        Assert.Equal(7, linhas.Count);

        foreach (var l in linhas.Where(l => l.Conjunto != "Fibonacci"))
        {
            Assert.Equal(0, l.ErrouComVirgula);
            Assert.Equal(0, l.AssimetricoComVirgula);
        }

        var fibonacci = linhas.Single(l => l.Conjunto == "Fibonacci");

        Assert.True((double)fibonacci.ErrouComVirgula / fibonacci.Trios > 0.3);
        Assert.True((double)fibonacci.AssimetricoComVirgula / fibonacci.Trios > 0.2);
    }

    /// <summary>
    /// E a TOLERANCIA erra praticamente o mesmo tanto: ela troca um erro por outro.
    /// </summary>
    [Fact]
    public void ATolerenciaErraOMesmoTanto()
    {
        var fibonacci = Medidas.Predicados(60).Single(l => l.Conjunto == "Fibonacci");

        Assert.InRange((double)fibonacci.ErrouComTolerancia / fibonacci.ErrouComVirgula, 0.9, 1.1);
    }

    /// <summary>
    /// A frase do texto: com o predicado com virgula e os pontos de Fibonacci, a
    /// saida deixa de ser convexa e deixa de conter os pontos.
    /// </summary>
    [Fact]
    public void OEstragoSoApareceEmFibonacci()
    {
        var linhas = Medidas.Estragos(200);

        Assert.Equal(21, linhas.Count);

        foreach (var l in linhas.Where(l => l.Conjunto != "Fibonacci"))
        {
            Assert.Equal(l.VerticesComExato, l.VerticesComVirgula);
            Assert.True(l.ConvexaComVirgula, $"{l.Conjunto}/{l.Algoritmo}");
            Assert.True(l.ContemComVirgula, $"{l.Conjunto}/{l.Algoritmo}");
        }

        Assert.Contains(linhas, l => l.Conjunto == "Fibonacci" && !l.ContemComVirgula);
    }

    /// <summary>
    /// A frase do texto: o embrulho de presente paga os pontos vezes os vertices, e
    /// no circulo isso e quatro milhoes de chamadas contra seis mil.
    /// </summary>
    [Fact]
    public void OEmbrulhoPagaOsPontosVezesOsVertices()
    {
        var linhas = Medidas.Custos(1_000);

        Assert.Equal(6, linhas.Count);

        var circulo = linhas.Where(l => l.Conjunto == "no circulo").ToList();

        var cadeia = circulo.Single(l => l.Algoritmo == "cadeia monotona");
        var embrulho = circulo.Single(l => l.Algoritmo == "embrulho de presente");

        Assert.Equal(cadeia.Pontos, cadeia.Vertices);
        Assert.True(embrulho.Chamadas > 100 * cadeia.Chamadas);
        Assert.InRange(embrulho.PorPonto, 0.8 * cadeia.Pontos, 1.2 * cadeia.Pontos);
    }

    /// <summary>
    /// E com pontos sorteados ele e barato: o custo dele depende da RESPOSTA, nao
    /// da entrada.
    /// </summary>
    [Fact]
    public void ComPontosSorteadosOEmbrulhoEhBarato()
    {
        var sorteados = Medidas.Custos(1_000).Where(l => l.Conjunto == "sorteados").ToList();

        var embrulho = sorteados.Single(l => l.Algoritmo == "embrulho de presente");

        Assert.True(embrulho.Vertices < 50);
        Assert.True(embrulho.PorPonto < 50, $"{embrulho.PorPonto:F1} chamadas por ponto");
    }

    /// <summary>
    /// A frase do texto: no circulo, um lado cresce LINEARMENTE e o outro AO
    /// QUADRADO.
    /// </summary>
    [Fact]
    public void UmLadoCresceLinearmenteEOOutroAoQuadrado()
    {
        var linhas = Medidas.Crescimentos();

        Assert.Equal(5, linhas.Count);

        for (var i = 1; i < linhas.Count; i++)
        {
            Assert.Equal(linhas[i].Pontos, linhas[i].Vertices);

            // dobrar os pontos dobra a cadeia monotona
            Assert.InRange((double)linhas[i].Monotona / linhas[i - 1].Monotona, 1.8, 2.2);

            // e quadruplica o embrulho
            Assert.InRange((double)linhas[i].Embrulho / linhas[i - 1].Embrulho, 3.5, 4.5);
        }
    }
}
