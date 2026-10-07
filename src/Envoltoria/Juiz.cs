namespace Conde.Envoltoria;

/// <summary>
/// O JUIZ deste repositorio: a DEFINICAO de vertice da envoltoria, conferida por
/// forca bruta.
///
/// Um ponto e vertice da envoltoria convexa se existe uma reta passando por ele
/// com TODOS os outros pontos de um lado so. Isso e a definicao, e ela da para
/// conferir ponto a ponto, sem algoritmo nenhum: para cada par de pontos, olha se
/// todos os outros ficam de um lado.
///
/// E cubico e nao da para melhorar, e isso nao e defeito: ele so roda em conjuntos
/// de algumas centenas de pontos, que e exatamente o tamanho em que um algoritmo
/// errado ainda devolve um poligono com cara de certo.
///
/// E ele usa o predicado EXATO sempre. Um juiz que usasse o predicado com virgula
/// erraria junto com o algoritmo que ele julga, e os dois concordariam.
/// </summary>
public static class Juiz
{
    /// <summary>
    /// Os VERTICES da envoltoria, pela definicao.
    ///
    /// Um ponto e vertice se existe um outro ponto tal que a reta entre os dois
    /// deixa todo o resto de um lado so, e o ponto nao e interior ao segmento.
    /// </summary>
    public static HashSet<Ponto> Vertices(IReadOnlyList<Ponto> pontos)
    {
        ArgumentNullException.ThrowIfNull(pontos);

        var unicos = pontos.Distinct().ToList();
        var vertices = new HashSet<Ponto>();

        if (unicos.Count <= 2) return [.. unicos];

        // SO O "A" entra, nunca o "B".
        //
        // A primeira versao daqui acrescentava os dois pontos do par que funcionava,
        // e isso punha na envoltoria os pontos do MEIO de uma aresta: com p, m e q
        // alinhados e p e q nas pontas, o par (p, m) deixa todo o resto de um lado
        // so, e o m entrava junto. Nos conjuntos com pontos alinhados o juiz passou
        // a discordar dos tres algoritmos ao mesmo tempo, que e o sinal de que o
        // errado era ele.
        foreach (var a in unicos)
            foreach (var b in unicos)
            {
                if (a.Equals(b)) continue;

                var temEsquerda = false;
                var temDireita = false;
                var temMaisLonge = false;

                foreach (var c in unicos)
                {
                    if (c.Equals(a) || c.Equals(b)) continue;

                    switch (Orientacao.Exata(a, b, c))
                    {
                        case Lado.Esquerda: temEsquerda = true; break;
                        case Lado.Direita: temDireita = true; break;
                        default:
                            // em linha: so atrapalha se estiver DO LADO DE FORA do
                            // segmento, porque ai o A deixa de ser extremo
                            if (ForaDoSegmento(a, b, c)) temMaisLonge = true;
                            break;
                    }
                }

                if (!(temEsquerda && temDireita) && !temMaisLonge)
                {
                    vertices.Add(a);
                    break;
                }
            }

        return vertices;
    }

    /// <summary>O ponto C, colinear com A e B, esta fora do segmento, do lado do A.</summary>
    private static bool ForaDoSegmento(Ponto a, Ponto b, Ponto c)
    {
        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var acx = c.X - a.X;
        var acy = c.Y - a.Y;

        // o produto escalar negativo quer dizer que C esta do lado oposto a B
        return (System.Numerics.BigInteger)abx * acx + (System.Numerics.BigInteger)aby * acy < 0;
    }

    /// <summary>
    /// A envoltoria devolvida e um POLIGONO CONVEXO, em sentido anti-horario.
    ///
    /// Esta e a conferencia que pega o estrago do predicado com virgula: o resultado
    /// continua sendo uma lista de pontos, com o tamanho plausivel, e ele deixa de
    /// virar sempre para o mesmo lado.
    /// </summary>
    public static bool EhConvexaEmSentidoAntiHorario(IReadOnlyList<Ponto> envoltoria)
    {
        ArgumentNullException.ThrowIfNull(envoltoria);
        if (envoltoria.Count <= 2) return true;

        for (var i = 0; i < envoltoria.Count; i++)
        {
            var a = envoltoria[i];
            var b = envoltoria[(i + 1) % envoltoria.Count];
            var c = envoltoria[(i + 2) % envoltoria.Count];

            if (Orientacao.Exata(a, b, c) != Lado.Esquerda) return false;
        }

        return true;
    }

    /// <summary>
    /// TODO ponto do conjunto esta dentro da envoltoria ou nela.
    ///
    /// E a outra metade da definicao: a de cima diz que o contorno e convexo, esta
    /// diz que ele CONTEM tudo. As duas juntas caracterizam a resposta certa.
    /// </summary>
    public static bool ContemTodosOsPontos(IReadOnlyList<Ponto> envoltoria, IReadOnlyList<Ponto> pontos)
    {
        ArgumentNullException.ThrowIfNull(envoltoria);
        ArgumentNullException.ThrowIfNull(pontos);

        if (envoltoria.Count <= 2) return true;

        foreach (var p in pontos)
        {
            for (var i = 0; i < envoltoria.Count; i++)
            {
                var a = envoltoria[i];
                var b = envoltoria[(i + 1) % envoltoria.Count];

                if (Orientacao.Exata(a, b, p) == Lado.Direita) return false;
            }
        }

        return true;
    }

    /// <summary>
    /// A envoltoria esta CERTA: ela e convexa, contem tudo, e os vertices dela sao
    /// exatamente os que a definicao diz.
    /// </summary>
    public static bool EstaCerta(IReadOnlyList<Ponto> envoltoria, IReadOnlyList<Ponto> pontos)
    {
        ArgumentNullException.ThrowIfNull(envoltoria);
        ArgumentNullException.ThrowIfNull(pontos);

        if (!EhConvexaEmSentidoAntiHorario(envoltoria)) return false;
        if (!ContemTodosOsPontos(envoltoria, pontos)) return false;

        return envoltoria.ToHashSet().SetEquals(Vertices(pontos));
    }

    public sealed record Relatorio(int Pontos, int Vertices, bool Convexa, bool Contem, bool BateComADefinicao);

    public static Relatorio Conferir(IReadOnlyList<Ponto> envoltoria, IReadOnlyList<Ponto> pontos)
    {
        ArgumentNullException.ThrowIfNull(envoltoria);
        ArgumentNullException.ThrowIfNull(pontos);

        return new Relatorio(
            pontos.Distinct().Count(),
            envoltoria.Count,
            EhConvexaEmSentidoAntiHorario(envoltoria),
            ContemTodosOsPontos(envoltoria, pontos),
            envoltoria.ToHashSet().SetEquals(Vertices(pontos)));
    }
}
