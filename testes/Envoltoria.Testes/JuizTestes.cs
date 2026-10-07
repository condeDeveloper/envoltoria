using Conde.Envoltoria;
using Xunit;

namespace Conde.Envoltoria.Testes;

/// <summary>
/// O juiz tambem precisa de juiz.
///
/// Todo veredicto deste repositorio e "a envoltoria bate com a definicao". Se a
/// forca bruta tivesse um erro, os tres algoritmos seriam julgados contra uma
/// referencia errada e os tres discordariam dela ao mesmo tempo, que e exatamente
/// o sinal de que o errado e o juiz.
/// </summary>
public class JuizTestes
{
    [Fact]
    public void OsVerticesDeUmQuadradoSaoOsQuatroCantos()
    {
        var pontos = new List<Ponto>
        {
            new(0, 0), new(10, 0), new(10, 10), new(0, 10),
            new(5, 5), new(1, 1), new(9, 9),
        };

        Assert.Equal(
            [new Ponto(0, 0), new Ponto(0, 10), new Ponto(10, 0), new Ponto(10, 10)],
            Juiz.Vertices(pontos).OrderBy(p => p.X).ThenBy(p => p.Y));
    }

    /// <summary>
    /// O ACHADO QUE ME CORRIGIU: o ponto do MEIO de uma aresta nao e vertice.
    ///
    /// A primeira versao do juiz acrescentava os DOIS pontos do par que funcionava,
    /// e isso punha na envoltoria os pontos do meio: com p, m e q alinhados e p e q
    /// nas pontas, o par (p, m) deixa todo o resto de um lado so, e o m entrava
    /// junto.
    ///
    /// Nos conjuntos com pontos alinhados, o juiz passou a discordar dos TRES
    /// algoritmos ao mesmo tempo, e tres implementacoes independentes concordando
    /// entre si contra o juiz e o sinal de que o errado e ele.
    /// </summary>
    [Fact]
    public void OPontoDoMeioDeUmaArestaNaoEhVertice()
    {
        var pontos = new List<Ponto>
        {
            new(0, 0), new(5, 0), new(10, 0),
            new(10, 10), new(0, 10),
        };

        var vertices = Juiz.Vertices(pontos);

        Assert.Equal(4, vertices.Count);
        Assert.DoesNotContain(new Ponto(5, 0), vertices);
    }

    [Fact]
    public void ComPoucosPontosTodosSaoVertices()
    {
        Assert.Single(Juiz.Vertices([new Ponto(1, 1)]));
        Assert.Equal(2, Juiz.Vertices([new Ponto(1, 1), new Ponto(2, 2)]).Count);
        Assert.Equal(3, Juiz.Vertices([new Ponto(0, 0), new Ponto(1, 0), new Ponto(0, 1)]).Count);
    }

    /// <summary>
    /// A conferencia de CONVEXIDADE pega um poligono que vira para os dois lados.
    /// </summary>
    [Fact]
    public void AConferenciaDeConvexidadePegaOPoligonoTorto()
    {
        var quadrado = new List<Ponto> { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };

        Assert.True(Juiz.EhConvexaEmSentidoAntiHorario(quadrado));

        // trocando dois vertices de lugar, ele deixa de ser convexo
        var torto = new List<Ponto> { new(0, 0), new(10, 10), new(10, 0), new(0, 10) };

        Assert.False(Juiz.EhConvexaEmSentidoAntiHorario(torto));

        // e no sentido horario tambem nao passa
        Assert.False(Juiz.EhConvexaEmSentidoAntiHorario(Enumerable.Reverse(quadrado).ToList()));
    }

    /// <summary>
    /// A conferencia de CONTEM pega um ponto que ficou de fora.
    ///
    /// Ela e a outra metade: a de cima diz que o contorno e convexo, esta diz que
    /// ele contem tudo. As duas juntas caracterizam a resposta certa, e nenhuma
    /// delas sozinha basta.
    /// </summary>
    [Fact]
    public void AConferenciaDeContemPegaOPontoDeFora()
    {
        var quadrado = new List<Ponto> { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };

        Assert.True(Juiz.ContemTodosOsPontos(quadrado, [new Ponto(5, 5), new Ponto(0, 0), new Ponto(10, 10)]));
        Assert.False(Juiz.ContemTodosOsPontos(quadrado, [new Ponto(11, 5)]));
        Assert.False(Juiz.ContemTodosOsPontos(quadrado, [new Ponto(-1, -1)]));
    }

    /// <summary>
    /// Um TRIANGULO sozinho ja mostra que as duas conferencias sao independentes:
    /// ele e convexo e nao contem um ponto de fora.
    /// </summary>
    [Fact]
    public void AsDuasConferenciasSaoIndependentes()
    {
        var triangulo = new List<Ponto> { new(0, 0), new(10, 0), new(0, 10) };
        var pontos = new List<Ponto> { new(0, 0), new(10, 0), new(0, 10), new(9, 9) };

        Assert.True(Juiz.EhConvexaEmSentidoAntiHorario(triangulo));
        Assert.False(Juiz.ContemTodosOsPontos(triangulo, pontos));
        Assert.False(Juiz.EstaCerta(triangulo, pontos));
    }

    /// <summary>
    /// O ESTRAGO: com o predicado com virgula e os pontos de Fibonacci, a saida
    /// deixa de ser um poligono convexo.
    ///
    /// E ela continua sendo uma lista de pontos, com o tamanho plausivel, e nada
    /// avisa. O algoritmo nao estoura, nao devolve vazio, nao lanca excecao: ele
    /// devolve uma coisa que nao e uma envoltoria.
    /// </summary>
    [Fact]
    public void ComOPredicadoComVirgulaASaidaDeixaDeSerConvexa()
    {
        var pontos = Conjuntos.Fibonacci(60);
        var estragou = 0;

        foreach (var algoritmo in Algoritmo.Todos())
        {
            var comExato = algoritmo.Envolver(pontos, Orientacao.Exata);

            Assert.True(Juiz.EstaCerta(comExato, pontos), algoritmo.Nome);

            List<Ponto> comVirgula;
            try
            {
                comVirgula = algoritmo.Envolver(pontos, Orientacao.ComVirgula);
            }
            catch (InvalidOperationException)
            {
                // a ordenacao recusa um comparador que se contradiz
                estragou++;
                continue;
            }

            if (!Juiz.EstaCerta(comVirgula, pontos)) estragou++;
        }

        Assert.Equal(3, estragou);
    }

    /// <summary>
    /// E em conjuntos de coordenadas pequenas o predicado com virgula da a MESMA
    /// envoltoria que o exato: o estrago depende da escala, nao do algoritmo.
    /// </summary>
    [Fact]
    public void ComCoordenadasPequenasOsDoisPredicadosDaoAMesmaEnvoltoria()
    {
        foreach (var (nome, pontos) in new[]
        {
            ("sorteados", Conjuntos.Sorteados(300)),
            ("na grade", Conjuntos.NaGrade(300)),
            ("no circulo", Conjuntos.NoCirculo(300)),
        })
            foreach (var algoritmo in Algoritmo.Todos())
                Assert.Equal(
                    algoritmo.Envolver(pontos, Orientacao.Exata),
                    algoritmo.Envolver(pontos, Orientacao.ComVirgula));
    }

    /// <summary>
    /// O relatorio junta tudo e concorda com cada conferencia feita em separado.
    /// </summary>
    [Fact]
    public void ORelatorioConcordaComAsConferenciasSeparadas()
    {
        foreach (var (nome, pontos) in Conjuntos.Todos(100))
        {
            var envoltoria = Algoritmo.CadeiaMonotona().Envolver(pontos);
            var relatorio = Juiz.Conferir(envoltoria, pontos);

            Assert.Equal(pontos.Distinct().Count(), relatorio.Pontos);
            Assert.Equal(envoltoria.Count, relatorio.Vertices);
            Assert.Equal(Juiz.EhConvexaEmSentidoAntiHorario(envoltoria), relatorio.Convexa);
            Assert.Equal(Juiz.ContemTodosOsPontos(envoltoria, pontos), relatorio.Contem);
        }
    }
}
