namespace Conde.Envoltoria;

/// <summary>
/// Um ALGORITMO de envoltoria convexa: dados os pontos, quais deles formam o
/// contorno.
///
/// Os tres daqui dao exatamente a mesma resposta e gastam coisas diferentes, e os
/// tres chamam o MESMO predicado de orientacao. Trocar o predicado troca a resposta
/// dos tres de uma vez, e e isso que torna a medida do predicado a medida mais
/// importante do repositorio.
/// </summary>
public abstract record Algoritmo(string Nome)
{
    /// <summary>A envoltoria, em sentido anti-horario, comecando pelo ponto mais baixo.</summary>
    public abstract List<Ponto> Envolver(IReadOnlyList<Ponto> pontos, Func<Ponto, Ponto, Ponto, Lado> predicado);

    /// <summary>Quantas vezes o predicado foi chamado na ultima vez.</summary>
    public long Chamadas { get; protected set; }

    public sealed override string ToString() => Nome;

    public List<Ponto> Envolver(IReadOnlyList<Ponto> pontos) => Envolver(pontos, Orientacao.Exata);

    /// <summary>
    /// A CADEIA MONOTONA de Andrew, de 1979: ordena e varre duas vezes.
    ///
    /// Ela e a mais simples das tres de ordenar e varrer: ordena os pontos por X,
    /// monta a metade de baixo da esquerda para a direita e a de cima da direita
    /// para a esquerda. Cada ponto entra e sai da pilha no maximo uma vez, entao a
    /// varredura custa o numero de pontos e a ordenacao domina.
    /// </summary>
    public static Algoritmo CadeiaMonotona() => new PorCadeia();

    /// <summary>
    /// O EMBRULHO DE PRESENTE de Jarvis, de 1973: a partir do mais baixo, sempre o
    /// ponto mais a direita.
    ///
    /// Ele e o mais facil de explicar e o unico cujo custo depende da RESPOSTA: ele
    /// gasta o numero de pontos vezes o numero de vertices da envoltoria. Com dez
    /// mil pontos sorteados num quadrado, a envoltoria tem umas poucas dezenas de
    /// vertices e ele e otimo. Com dez mil pontos num circulo, todos sao vertices e
    /// ele custa cem milhoes de passos.
    /// </summary>
    public static Algoritmo EmbrulhoDePresente() => new PorEmbrulho();

    /// <summary>
    /// A VARREDURA DE GRAHAM, de 1972: o primeiro algoritmo de envoltoria em n log
    /// n da historia.
    ///
    /// Ela ordena os pontos pelo ANGULO em volta do mais baixo e varre uma vez,
    /// desempilhando sempre que a curva vira para o lado errado. A ordenacao por
    /// angulo e a parte delicada: ela compara dois pontos pelo predicado de
    /// orientacao, nao por um numero.
    /// </summary>
    public static Algoritmo VarreduraDeGraham() => new PorGraham();

    public static IEnumerable<Algoritmo> Todos()
    {
        yield return CadeiaMonotona();
        yield return EmbrulhoDePresente();
        yield return VarreduraDeGraham();
    }

    /// <summary>O ponto mais baixo, desempatando pelo mais a esquerda.</summary>
    protected static Ponto MaisBaixo(IReadOnlyList<Ponto> pontos)
    {
        var melhor = pontos[0];

        foreach (var p in pontos)
            if (p.Y < melhor.Y || (p.Y == melhor.Y && p.X < melhor.X)) melhor = p;

        return melhor;
    }

    /// <summary>Gira a lista para comecar pelo ponto mais baixo, mantendo o sentido.</summary>
    protected static List<Ponto> ComecarPeloMaisBaixo(List<Ponto> envoltoria)
    {
        if (envoltoria.Count <= 1) return envoltoria;

        var inicio = 0;
        for (var i = 1; i < envoltoria.Count; i++)
            if (envoltoria[i].Y < envoltoria[inicio].Y ||
                (envoltoria[i].Y == envoltoria[inicio].Y && envoltoria[i].X < envoltoria[inicio].X))
                inicio = i;

        return [.. envoltoria.Skip(inicio), .. envoltoria.Take(inicio)];
    }

    private sealed record PorCadeia() : Algoritmo("cadeia monotona")
    {
        public override List<Ponto> Envolver(IReadOnlyList<Ponto> pontos, Func<Ponto, Ponto, Ponto, Lado> predicado)
        {
            ArgumentNullException.ThrowIfNull(pontos);
            ArgumentNullException.ThrowIfNull(predicado);

            Chamadas = 0;

            var unicos = pontos.Distinct().OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            if (unicos.Count <= 2) return unicos;

            var baixo = new List<Ponto>();
            var cima = new List<Ponto>();

            foreach (var p in unicos)
            {
                while (baixo.Count >= 2 && Virou(predicado, baixo[^2], baixo[^1], p)) baixo.RemoveAt(baixo.Count - 1);
                baixo.Add(p);
            }

            for (var i = unicos.Count - 1; i >= 0; i--)
            {
                var p = unicos[i];

                while (cima.Count >= 2 && Virou(predicado, cima[^2], cima[^1], p)) cima.RemoveAt(cima.Count - 1);
                cima.Add(p);
            }

            baixo.RemoveAt(baixo.Count - 1);
            cima.RemoveAt(cima.Count - 1);

            return ComecarPeloMaisBaixo([.. baixo, .. cima]);
        }

        private bool Virou(Func<Ponto, Ponto, Ponto, Lado> predicado, Ponto a, Ponto b, Ponto c)
        {
            Chamadas++;

            // o "ou em linha" tira os pontos colineares da envoltoria: e uma
            // escolha de projeto, e ela precisa ser a mesma nos tres algoritmos
            // para a comparacao fazer sentido
            return predicado(a, b, c) != Lado.Esquerda;
        }
    }

    private sealed record PorEmbrulho() : Algoritmo("embrulho de presente")
    {
        public override List<Ponto> Envolver(IReadOnlyList<Ponto> pontos, Func<Ponto, Ponto, Ponto, Lado> predicado)
        {
            ArgumentNullException.ThrowIfNull(pontos);
            ArgumentNullException.ThrowIfNull(predicado);

            Chamadas = 0;

            var unicos = pontos.Distinct().ToList();
            if (unicos.Count <= 2) return [.. unicos.OrderBy(p => p.X).ThenBy(p => p.Y)];

            var inicio = MaisBaixo(unicos);
            var envoltoria = new List<Ponto>();
            var atual = inicio;

            do
            {
                envoltoria.Add(atual);

                var candidato = unicos[0].Equals(atual) && unicos.Count > 1 ? unicos[1] : unicos[0];

                foreach (var p in unicos)
                {
                    if (p.Equals(atual)) continue;

                    Chamadas++;
                    var lado = predicado(atual, candidato, p);

                    // o mais a direita de todos, e em caso de empate o mais longe:
                    // sem o desempate por distancia, os pontos colineares entram na
                    // envoltoria fora de ordem e ela deixa de ser um poligono
                    if (lado == Lado.Direita ||
                        (lado == Lado.EmLinha && atual.DistanciaAoQuadrado(p) > atual.DistanciaAoQuadrado(candidato)))
                        candidato = p;
                }

                atual = candidato;
            }
            while (!atual.Equals(inicio) && envoltoria.Count <= unicos.Count);

            return envoltoria;
        }
    }

    private sealed record PorGraham() : Algoritmo("varredura de Graham")
    {
        public override List<Ponto> Envolver(IReadOnlyList<Ponto> pontos, Func<Ponto, Ponto, Ponto, Lado> predicado)
        {
            ArgumentNullException.ThrowIfNull(pontos);
            ArgumentNullException.ThrowIfNull(predicado);

            Chamadas = 0;

            var unicos = pontos.Distinct().ToList();
            if (unicos.Count <= 2) return [.. unicos.OrderBy(p => p.X).ThenBy(p => p.Y)];

            var inicio = MaisBaixo(unicos);
            var resto = unicos.Where(p => !p.Equals(inicio)).ToList();

            // a ordenacao por ANGULO, feita com o predicado e nao com um numero:
            // calcular o angulo com arco tangente trocaria um problema exato por um
            // aproximado, e e por isso que a comparacao e geometrica
            resto.Sort((p, q) =>
            {
                Chamadas++;
                var lado = predicado(inicio, p, q);

                if (lado == Lado.Esquerda) return -1;
                if (lado == Lado.Direita) return 1;

                return inicio.DistanciaAoQuadrado(p).CompareTo(inicio.DistanciaAoQuadrado(q));
            });

            var pilha = new List<Ponto> { inicio };

            foreach (var p in resto)
            {
                while (pilha.Count >= 2 && Virou(predicado, pilha[^2], pilha[^1], p)) pilha.RemoveAt(pilha.Count - 1);
                pilha.Add(p);
            }

            return ComecarPeloMaisBaixo(pilha);
        }

        private bool Virou(Func<Ponto, Ponto, Ponto, Lado> predicado, Ponto a, Ponto b, Ponto c)
        {
            Chamadas++;
            return predicado(a, b, c) != Lado.Esquerda;
        }
    }
}
