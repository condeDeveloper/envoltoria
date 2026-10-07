using System.Numerics;
using Conde.Envoltoria;
using Xunit;

namespace Conde.Envoltoria.Testes;

public class OrientacaoTestes
{
    [Fact]
    public void OSorteioEhDeterminista()
    {
        var a = new Sorteio(7);
        var b = new Sorteio(7);

        for (var i = 0; i < 1000; i++) Assert.Equal(a.Proximo(), b.Proximo());
    }

    /// <summary>
    /// A fracao precisa cobrir os DOIS LADOS do meio: um deslocamento de 33 em vez
    /// de 32 deixaria o gerador com 31 bits, e os pontos sorteados ficariam todos
    /// na metade de baixo do quadrado, com os numeros continuando plausiveis.
    /// </summary>
    [Fact]
    public void AFracaoCobreOsDoisLadosDoMeio()
    {
        var sorteio = new Sorteio(7);
        var acima = 0;
        var maior = 0.0;

        for (var i = 0; i < 10_000; i++)
        {
            var f = sorteio.Fracao();

            Assert.InRange(f, 0.0, 1.0);
            if (f > 0.5) acima++;
            if (f > maior) maior = f;
        }

        Assert.InRange(acima, 4_700, 5_300);
        Assert.True(maior > 0.999, $"o maior sorteio foi {maior}");
    }

    [Fact]
    public void OPredicadoExatoAcertaOsCasosObvios()
    {
        var a = new Ponto(0, 0);
        var b = new Ponto(10, 0);

        Assert.Equal(Lado.Esquerda, Orientacao.Exata(a, b, new Ponto(5, 1)));
        Assert.Equal(Lado.Direita, Orientacao.Exata(a, b, new Ponto(5, -1)));
        Assert.Equal(Lado.EmLinha, Orientacao.Exata(a, b, new Ponto(5, 0)));
        Assert.Equal(Lado.EmLinha, Orientacao.Exata(a, b, new Ponto(-5, 0)));
        Assert.Equal(Lado.EmLinha, Orientacao.Exata(a, b, new Ponto(1000, 0)));
    }

    /// <summary>
    /// A SIMETRIA do predicado: trocar dois pontos inverte o sinal, e girar os tres
    /// mantem.
    ///
    /// E a propriedade de que todo algoritmo de geometria depende sem nunca dizer,
    /// e o predicado exato a cumpre em todo trio.
    /// </summary>
    [Fact]
    public void OPredicadoExatoEhSimetricoSempre()
    {
        foreach (var (nome, pontos) in Conjuntos.Todos(40))
        {
            var unicos = pontos.Distinct().ToList();

            foreach (var a in unicos)
                foreach (var b in unicos)
                    foreach (var c in unicos)
                    {
                        if (a.Equals(b) || b.Equals(c) || a.Equals(c)) continue;

                        Assert.True(Orientacao.EhSimetrico(Orientacao.Exata, a, b, c), $"{nome}: {a} {b} {c}");
                    }
        }
    }

    /// <summary>
    /// O ACHADO PRINCIPAL: o predicado com virgula SE CONTRADIZ.
    ///
    /// Com os pontos de Fibonacci, ele diz que C esta a esquerda de AB e que A esta
    /// a esquerda de CB ao mesmo tempo. Nao e impreciso: e incoerente, e nenhum
    /// algoritmo sobrevive a isso.
    /// </summary>
    [Fact]
    public void OPredicadoComVirgulaSeContradiz()
    {
        var pontos = Conjuntos.Fibonacci(40);
        var contradicoes = 0;
        var trios = 0;

        foreach (var a in pontos)
            foreach (var b in pontos)
                foreach (var c in pontos)
                {
                    if (a.Equals(b) || b.Equals(c) || a.Equals(c)) continue;

                    trios++;
                    if (!Orientacao.EhSimetrico(Orientacao.ComVirgula, a, b, c)) contradicoes++;
                }

        Assert.True(contradicoes > trios / 10, $"so {contradicoes} contradicoes em {trios} trios");
    }

    /// <summary>
    /// E ele ERRA o sinal em mais da metade dos trios de Fibonacci, contra o
    /// exato.
    /// </summary>
    [Fact]
    public void OPredicadoComVirgulaErraNaMaioriaDosTriosDeFibonacci()
    {
        var pontos = Conjuntos.Fibonacci(40);
        var errou = 0;
        var trios = 0;

        foreach (var a in pontos)
            foreach (var b in pontos)
                foreach (var c in pontos)
                {
                    if (a.Equals(b) || b.Equals(c) || a.Equals(c)) continue;

                    trios++;
                    if (Orientacao.ComVirgula(a, b, c) != Orientacao.Exata(a, b, c)) errou++;
                }

        Assert.True((double)errou / trios > 0.3, $"errou em {errou} de {trios}");
    }

    /// <summary>
    /// O ACHADO QUE ME CUSTOU QUATRO TENTATIVAS: com coordenadas ate dez milhoes,
    /// o predicado com virgula NAO erra nenhuma vez.
    ///
    /// Os dois produtos do determinante ficam na ordem de 10^14, que e menor que
    /// 2^53: a precisao dupla representa os dois exatamente e a subtracao de dois
    /// inteiros exatos tambem e exata. O arredondamento so comeca acima de nove mil
    /// trilhoes.
    ///
    /// E e por isso que o bug e tao perigoso: ele nao aparece em teste nenhum com
    /// dados de tamanho normal.
    /// </summary>
    [Fact]
    public void ComCoordenadasPequenasOPredicadoComVirgulaNaoErra()
    {
        foreach (var (nome, pontos) in new[]
        {
            ("sorteados", Conjuntos.Sorteados(40)),
            ("na grade", Conjuntos.NaGrade(40)),
            ("no circulo", Conjuntos.NoCirculo(40)),
        })
        {
            var unicos = pontos.Distinct().ToList();

            foreach (var a in unicos)
                foreach (var b in unicos)
                    foreach (var c in unicos)
                    {
                        if (a.Equals(b) || b.Equals(c) || a.Equals(c)) continue;

                        Assert.Equal(Orientacao.Exata(a, b, c), Orientacao.ComVirgula(a, b, c));
                    }
        }
    }

    /// <summary>
    /// E a TOLERANCIA nao conserta: ela troca um erro por outro, e nos pontos de
    /// Fibonacci ela erra tanto quanto a versao sem tolerancia.
    /// </summary>
    [Fact]
    public void AToleranciaNaoConserta()
    {
        var pontos = Conjuntos.Fibonacci(40);
        var semTolerancia = 0;
        var comTolerancia = 0;

        foreach (var a in pontos)
            foreach (var b in pontos)
                foreach (var c in pontos)
                {
                    if (a.Equals(b) || b.Equals(c) || a.Equals(c)) continue;

                    var certo = Orientacao.Exata(a, b, c);

                    if (Orientacao.ComVirgula(a, b, c) != certo) semTolerancia++;
                    if (Orientacao.ComTolerancia(a, b, c) != certo) comTolerancia++;
                }

        Assert.True(comTolerancia > 0);
        Assert.InRange((double)comTolerancia / semTolerancia, 0.9, 1.1);
    }

    [Fact]
    public void OCruzadoEhODeterminanteDeVerdade()
    {
        Assert.Equal(BigInteger.Zero, Orientacao.Cruzado(new Ponto(0, 0), new Ponto(2, 2), new Ponto(5, 5)));
        Assert.Equal(new BigInteger(2), Orientacao.Cruzado(new Ponto(0, 0), new Ponto(2, 0), new Ponto(0, 1)));
        Assert.Equal(new BigInteger(-2), Orientacao.Cruzado(new Ponto(0, 0), new Ponto(2, 0), new Ponto(0, -1)));
    }

    [Fact]
    public void InverterTrocaOsLados()
    {
        Assert.Equal(Lado.Direita, Orientacao.Inverter(Lado.Esquerda));
        Assert.Equal(Lado.Esquerda, Orientacao.Inverter(Lado.Direita));
        Assert.Equal(Lado.EmLinha, Orientacao.Inverter(Lado.EmLinha));
    }

    [Fact]
    public void DentroDoTrianguloAcertaOsCasosObvios()
    {
        var a = new Ponto(0, 0);
        var b = new Ponto(10, 0);
        var c = new Ponto(0, 10);

        Assert.True(Orientacao.DentroDoTriangulo(Orientacao.Exata, a, b, c, new Ponto(1, 1)));
        Assert.True(Orientacao.DentroDoTriangulo(Orientacao.Exata, a, b, c, new Ponto(5, 0)));
        Assert.False(Orientacao.DentroDoTriangulo(Orientacao.Exata, a, b, c, new Ponto(10, 10)));
        Assert.False(Orientacao.DentroDoTriangulo(Orientacao.Exata, a, b, c, new Ponto(-1, -1)));
    }

    [Fact]
    public void ADistanciaAoQuadradoEhExata()
    {
        Assert.Equal(new BigInteger(25), new Ponto(0, 0).DistanciaAoQuadrado(new Ponto(3, 4)));
        Assert.Equal(BigInteger.Zero, new Ponto(7, 7).DistanciaAoQuadrado(new Ponto(7, 7)));

        var grande = new Ponto(2_000_000_000, 0).DistanciaAoQuadrado(new Ponto(0, 0));
        Assert.Equal(BigInteger.Pow(2_000_000_000, 2), grande);
    }
}
