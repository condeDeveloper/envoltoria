using Conde.Envoltoria;
using Xunit;

namespace Conde.Envoltoria.Testes;

public class AlgoritmosTestes
{
    /// <summary>
    /// OS TRES ALGORITMOS dao exatamente a mesma envoltoria, em todos os conjuntos.
    ///
    /// E a mesma lista, na mesma ordem, comecando pelo mesmo ponto: eles nao sao
    /// "equivalentes a menos de rotacao", eles sao iguais.
    /// </summary>
    [Fact]
    public void OsTresDaoAMesmaEnvoltoria()
    {
        foreach (var (nome, pontos) in Conjuntos.Todos(300))
        {
            var cadeia = Algoritmo.CadeiaMonotona().Envolver(pontos);
            var embrulho = Algoritmo.EmbrulhoDePresente().Envolver(pontos);
            var graham = Algoritmo.VarreduraDeGraham().Envolver(pontos);

            Assert.Equal(cadeia, embrulho);
            Assert.Equal(cadeia, graham);
        }
    }

    /// <summary>
    /// E a envoltoria esta CERTA pela definicao: convexa, contendo tudo, e com
    /// exatamente os vertices que a forca bruta diz.
    /// </summary>
    [Fact]
    public void AEnvoltoriaEstaCertaPelaDefinicao()
    {
        foreach (var (nome, pontos) in Conjuntos.Todos(150))
            foreach (var algoritmo in Algoritmo.Todos())
            {
                var envoltoria = algoritmo.Envolver(pontos);

                Assert.True(Juiz.EstaCerta(envoltoria, pontos), $"{nome} com {algoritmo.Nome}");
            }
    }

    /// <summary>
    /// E em conjuntos SORTEADOS, muitos deles, tambem: nao e um caso feliz.
    /// </summary>
    [Fact]
    public void AEnvoltoriaEstaCertaEmMuitosConjuntosSorteados()
    {
        for (ulong semente = 1; semente <= 60; semente++)
        {
            var pontos = Conjuntos.Sorteados(40, 500, semente);

            foreach (var algoritmo in Algoritmo.Todos())
                Assert.True(Juiz.EstaCerta(algoritmo.Envolver(pontos), pontos), $"semente {semente}, {algoritmo.Nome}");
        }
    }

    /// <summary>
    /// Os PONTOS COLINEARES do meio de uma aresta NAO entram na envoltoria, e isso
    /// e uma escolha de projeto que precisa ser a mesma nos tres.
    /// </summary>
    [Fact]
    public void OsPontosColinearesDoMeioNaoEntram()
    {
        var pontos = new List<Ponto>
        {
            new(0, 0), new(5, 0), new(10, 0),
            new(10, 10), new(5, 10), new(0, 10),
            new(5, 5),
        };

        foreach (var algoritmo in Algoritmo.Todos())
        {
            var envoltoria = algoritmo.Envolver(pontos);

            Assert.Equal(4, envoltoria.Count);
            Assert.DoesNotContain(new Ponto(5, 0), envoltoria);
            Assert.DoesNotContain(new Ponto(5, 5), envoltoria);
        }
    }

    [Fact]
    public void ComUmOuDoisPontosNaoHaPoligono()
    {
        foreach (var algoritmo in Algoritmo.Todos())
        {
            Assert.Single(algoritmo.Envolver([new Ponto(1, 1)]));
            Assert.Equal(2, algoritmo.Envolver([new Ponto(1, 1), new Ponto(5, 5)]).Count);
            Assert.Single(algoritmo.Envolver([new Ponto(1, 1), new Ponto(1, 1)]));
        }
    }

    [Fact]
    public void ComTodosEmLinhaRetaSobramAsDuasPontas()
    {
        var pontos = Conjuntos.EmLinhaReta(20);

        foreach (var algoritmo in Algoritmo.Todos())
        {
            var envoltoria = algoritmo.Envolver(pontos);

            Assert.Equal(2, envoltoria.Count);
            Assert.Contains(pontos[0], envoltoria);
            Assert.Contains(pontos[^1], envoltoria);
        }
    }

    /// <summary>
    /// A envoltoria sempre COMECA pelo ponto mais baixo, desempatando pelo mais a
    /// esquerda: sem uma ordem fixa, comparar as tres saidas nao faria sentido.
    /// </summary>
    [Fact]
    public void AEnvoltoriaComecaPeloPontoMaisBaixo()
    {
        foreach (var (nome, pontos) in Conjuntos.Todos(200))
            foreach (var algoritmo in Algoritmo.Todos())
            {
                var envoltoria = algoritmo.Envolver(pontos);
                if (envoltoria.Count < 3) continue;

                var maisBaixo = envoltoria.OrderBy(p => p.Y).ThenBy(p => p.X).First();

                Assert.Equal(maisBaixo, envoltoria[0]);
            }
    }

    /// <summary>
    /// O ACHADO DO CUSTO: o embrulho de presente paga o numero de pontos vezes o
    /// numero de VERTICES, e isso e otimo quando a envoltoria e pequena e
    /// desastroso quando ela e grande.
    ///
    /// Com dois mil pontos no circulo, onde todos sao vertices, ele gasta quatro
    /// milhoes de chamadas contra seis mil da cadeia monotona.
    /// </summary>
    [Fact]
    public void OEmbrulhoPagaOsPontosVezesOsVertices()
    {
        var pontos = Conjuntos.NoCirculo(500);

        var cadeia = Algoritmo.CadeiaMonotona();
        cadeia.Envolver(pontos);

        var embrulho = Algoritmo.EmbrulhoDePresente();
        embrulho.Envolver(pontos);

        Assert.True(embrulho.Chamadas > 100 * cadeia.Chamadas,
            $"cadeia {cadeia.Chamadas}, embrulho {embrulho.Chamadas}");

        // e a conta fechada: pontos vezes vertices
        Assert.InRange((double)embrulho.Chamadas / (pontos.Count * (double)pontos.Count), 0.8, 1.2);
    }

    /// <summary>
    /// E com a envoltoria PEQUENA ele ganha da varredura de Graham, que paga a
    /// ordenacao mesmo quando a resposta tem tres pontos.
    /// </summary>
    [Fact]
    public void ComEnvoltoriaPequenaOEmbrulhoCompensa()
    {
        var pontos = Conjuntos.Sorteados(2_000);

        var embrulho = Algoritmo.EmbrulhoDePresente();
        var envoltoria = embrulho.Envolver(pontos);

        var graham = Algoritmo.VarreduraDeGraham();
        graham.Envolver(pontos);

        Assert.True(envoltoria.Count < 50, $"a envoltoria tinha {envoltoria.Count} vertices");
        Assert.True(embrulho.Chamadas < 2 * graham.Chamadas);
    }

    /// <summary>
    /// A CADEIA MONOTONA e a mais barata das tres em chamadas do predicado, EM
    /// QUASE TODO conjunto: ela nao ordena por angulo e nao percorre a envoltoria de
    /// novo.
    ///
    /// O "quase" e o achado. Com os sessenta pontos de Fibonacci, cuja envoltoria
    /// tem TRES vertices, o embrulho de presente gasta 177 chamadas contra 202 da
    /// cadeia monotona: com a resposta minuscula, o custo dele (pontos vezes
    /// vertices) fica abaixo do custo de ordenar.
    ///
    /// E por isso que o embrulho de presente nao e uma curiosidade historica: ele e
    /// a escolha certa quando a envoltoria e pequena, e isso e comum.
    /// </summary>
    [Fact]
    public void ACadeiaMonotonaEhAMaisBarataQuandoAEnvoltoriaNaoEhMinuscula()
    {
        var excecoes = 0;

        foreach (var (nome, pontos) in Conjuntos.Todos(1_000))
        {
            var cadeia = Algoritmo.CadeiaMonotona();
            var envoltoria = cadeia.Envolver(pontos);

            foreach (var outro in new[] { Algoritmo.EmbrulhoDePresente(), Algoritmo.VarreduraDeGraham() })
            {
                outro.Envolver(pontos);

                if (cadeia.Chamadas <= outro.Chamadas) continue;

                // a unica excecao aceita e o embrulho com envoltoria minuscula
                Assert.Equal("embrulho de presente", outro.Nome);
                Assert.True(envoltoria.Count <= 4, $"{nome}: envoltoria de {envoltoria.Count} vertices");

                excecoes++;
            }
        }

        Assert.True(excecoes <= 2, $"{excecoes} excecoes");
    }

    /// <summary>
    /// E o embrulho de presente GANHA quando a envoltoria tem tres vertices: o
    /// custo dele depende da resposta, nao da entrada.
    /// </summary>
    [Fact]
    public void OEmbrulhoGanhaQuandoAEnvoltoriaEhMinuscula()
    {
        var pontos = Conjuntos.Fibonacci(60);

        var cadeia = Algoritmo.CadeiaMonotona();
        var envoltoria = cadeia.Envolver(pontos);

        var embrulho = Algoritmo.EmbrulhoDePresente();
        embrulho.Envolver(pontos);

        Assert.True(envoltoria.Count <= 4, $"a envoltoria tinha {envoltoria.Count} vertices");
        Assert.True(embrulho.Chamadas < cadeia.Chamadas,
            $"cadeia {cadeia.Chamadas}, embrulho {embrulho.Chamadas}");
    }

    [Fact]
    public void CadaAlgoritmoTemNomeESeImprimePeloNome()
    {
        foreach (var algoritmo in Algoritmo.Todos())
        {
            Assert.False(string.IsNullOrWhiteSpace(algoritmo.Nome));
            Assert.Equal(algoritmo.Nome, algoritmo.ToString());
        }

        Assert.Equal(3, Algoritmo.Todos().Count());
    }
}
