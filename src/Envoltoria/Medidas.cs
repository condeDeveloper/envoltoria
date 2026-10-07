namespace Conde.Envoltoria;

/// <summary>
/// As MEDIDAS: as tabelas que o medidor imprime e que os testes conferem.
///
/// Elas ficam aqui, e nao no medidor, para o teste poder afirmar sobre o MESMO
/// numero que aparece no texto. Tabela de README que ninguem testa envelhece
/// errado em silencio.
///
/// Nenhuma delas usa relogio. Todas sao chamadas do predicado, vertices ou
/// respostas erradas.
/// </summary>
public static class Medidas
{
    public sealed record LinhaJuiz(string Conjunto, string Algoritmo, int Pontos, int Vertices, bool Convexa, bool Contem, bool Bate);

    /// <summary>
    /// O JUIZ: os tres algoritmos contra a definicao, com o predicado EXATO.
    ///
    /// As tres ultimas colunas precisam ser todas "sim": a envoltoria e convexa,
    /// ela contem tudo, e os vertices dela sao exatamente os que a definicao diz.
    /// </summary>
    public static List<LinhaJuiz> Juiz(int pontos = 200)
    {
        var linhas = new List<LinhaJuiz>();

        foreach (var (nome, conjunto) in Conjuntos.Todos(pontos))
            foreach (var algoritmo in Algoritmo.Todos())
            {
                var envoltoria = algoritmo.Envolver(conjunto);
                var relatorio = Envoltoria.Juiz.Conferir(envoltoria, conjunto);

                linhas.Add(new LinhaJuiz(
                    nome,
                    algoritmo.Nome,
                    relatorio.Pontos,
                    relatorio.Vertices,
                    relatorio.Convexa,
                    relatorio.Contem,
                    relatorio.BateComADefinicao));
            }

        return linhas;
    }

    public sealed record LinhaPredicado(string Conjunto, int Trios, int ErrouComVirgula, int ErrouComTolerancia, int AssimetricoComVirgula);

    /// <summary>
    /// O PREDICADO com virgula contra o exato, em todos os trios de pontos.
    ///
    /// A ultima coluna e a pior: ela conta os trios em que o predicado com virgula
    /// se contradiz consigo mesmo, dizendo que C esta a esquerda de AB e que A esta
    /// a esquerda de CB ao mesmo tempo.
    /// </summary>
    public static List<LinhaPredicado> Predicados(int pontos = 60)
    {
        var linhas = new List<LinhaPredicado>();

        foreach (var (nome, conjunto) in Conjuntos.Todos(pontos))
        {
            var unicos = conjunto.Distinct().ToList();

            var trios = 0;
            var comVirgula = 0;
            var comTolerancia = 0;
            var assimetrico = 0;

            foreach (var a in unicos)
                foreach (var b in unicos)
                    foreach (var c in unicos)
                    {
                        if (a.Equals(b) || b.Equals(c) || a.Equals(c)) continue;

                        trios++;

                        var certo = Orientacao.Exata(a, b, c);

                        if (Orientacao.ComVirgula(a, b, c) != certo) comVirgula++;
                        if (Orientacao.ComTolerancia(a, b, c) != certo) comTolerancia++;
                        if (!Orientacao.EhSimetrico(Orientacao.ComVirgula, a, b, c)) assimetrico++;
                    }

            linhas.Add(new LinhaPredicado(nome, trios, comVirgula, comTolerancia, assimetrico));
        }

        return linhas;
    }

    public sealed record LinhaEstrago(string Conjunto, string Algoritmo, int VerticesComExato, int VerticesComVirgula, bool ConvexaComVirgula, bool ContemComVirgula);

    /// <summary>
    /// O ESTRAGO: o mesmo algoritmo com o predicado com virgula.
    ///
    /// A saida continua sendo uma lista de pontos com o tamanho plausivel. Ela so
    /// deixa de ser um poligono convexo, ou deixa de conter os pontos, e nada avisa.
    /// </summary>
    public static List<LinhaEstrago> Estragos(int pontos = 300)
    {
        var linhas = new List<LinhaEstrago>();

        foreach (var (nome, conjunto) in Conjuntos.Todos(pontos))
            foreach (var algoritmo in Algoritmo.Todos())
            {
                var comExato = algoritmo.Envolver(conjunto, Orientacao.Exata);

                List<Ponto> comVirgula;
                try
                {
                    comVirgula = algoritmo.Envolver(conjunto, Orientacao.ComVirgula);
                }
                catch (InvalidOperationException)
                {
                    // a ordenacao de Graham recusa um comparador que se contradiz
                    comVirgula = [];
                }

                linhas.Add(new LinhaEstrago(
                    nome,
                    algoritmo.Nome,
                    comExato.Count,
                    comVirgula.Count,
                    Envoltoria.Juiz.EhConvexaEmSentidoAntiHorario(comVirgula),
                    comVirgula.Count > 0 && Envoltoria.Juiz.ContemTodosOsPontos(comVirgula, conjunto)));
            }

        return linhas;
    }

    public sealed record LinhaCusto(string Conjunto, string Algoritmo, int Pontos, int Vertices, long Chamadas, double PorPonto);

    /// <summary>
    /// O CUSTO em chamadas do predicado, que e a operacao que domina os tres
    /// algoritmos.
    ///
    /// A linha do circulo e a que conta a historia: la todos os pontos sao vertices,
    /// e o embrulho de presente paga o numero de pontos vezes o numero de vertices.
    /// </summary>
    public static List<LinhaCusto> Custos(int pontos = 2_000)
    {
        var linhas = new List<LinhaCusto>();

        foreach (var (nome, conjunto) in new[]
        {
            ("sorteados", Conjuntos.Sorteados(pontos)),
            ("no circulo", Conjuntos.NoCirculo(pontos)),
        })
            foreach (var algoritmo in Algoritmo.Todos())
            {
                var envoltoria = algoritmo.Envolver(conjunto);

                linhas.Add(new LinhaCusto(
                    nome,
                    algoritmo.Nome,
                    conjunto.Distinct().Count(),
                    envoltoria.Count,
                    algoritmo.Chamadas,
                    (double)algoritmo.Chamadas / conjunto.Distinct().Count()));
            }

        return linhas;
    }

    public sealed record LinhaCrescimento(int Pontos, int Vertices, long Monotona, long Embrulho);

    /// <summary>
    /// O CUSTO conforme o conjunto cresce, no circulo, onde todos os pontos sao
    /// vertices.
    ///
    /// Um lado cresce linearmente e o outro ao quadrado.
    /// </summary>
    public static List<LinhaCrescimento> Crescimentos()
    {
        var linhas = new List<LinhaCrescimento>();

        foreach (var quantos in new[] { 100, 200, 400, 800, 1_600 })
        {
            var conjunto = Conjuntos.NoCirculo(quantos);

            var monotona = Algoritmo.CadeiaMonotona();
            var envoltoria = monotona.Envolver(conjunto);

            var embrulho = Algoritmo.EmbrulhoDePresente();
            embrulho.Envolver(conjunto);

            linhas.Add(new LinhaCrescimento(conjunto.Count, envoltoria.Count, monotona.Chamadas, embrulho.Chamadas));
        }

        return linhas;
    }
}
