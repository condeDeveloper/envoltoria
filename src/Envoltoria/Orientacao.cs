using System.Numerics;

namespace Conde.Envoltoria;

/// <summary>De que lado da reta AB esta o ponto C.</summary>
public enum Lado
{
    /// <summary>A curva vira para a esquerda: o determinante e positivo.</summary>
    Esquerda = 1,

    /// <summary>Os tres estao em linha reta: o determinante e zero.</summary>
    EmLinha = 0,

    /// <summary>A curva vira para a direita: o determinante e negativo.</summary>
    Direita = -1,
}

/// <summary>
/// O PREDICADO DE ORIENTACAO: a peca de que toda a geometria computacional
/// depende.
///
/// A pergunta e "indo de A para B, o ponto C fica a esquerda, a direita ou em
/// cima da reta?", e a resposta e o sinal de um determinante dois por dois:
///
///   (B.X - A.X) * (C.Y - A.Y) - (B.Y - A.Y) * (C.X - A.X)
///
/// Tres linhas de codigo, e todo algoritmo de envoltoria convexa, triangulacao,
/// localizacao de ponto e interseccao de segmentos chama isso milhares de vezes e
/// confia no SINAL.
///
/// E e nesse sinal que mora o problema. Com coordenadas grandes, os dois produtos
/// ficam quase iguais e a diferenca entre eles e pequena: e cancelamento
/// catastrofico, no lugar mais perigoso possivel. Uma resposta errada aqui nao da
/// um poligono um pouco torto: ela da uma coisa que nao e poligono.
///
/// Com coordenadas INTEIRAS e aritmetica exata, o determinante e exato e o sinal
/// esta certo sempre. Esta classe tem as duas versoes, e a medida mostra a
/// diferenca.
/// </summary>
public static class Orientacao
{
    /// <summary>
    /// O predicado EXATO, com inteiros grandes.
    ///
    /// Com coordenadas de 64 bits, o produto cabe em 128 e a subtracao tambem. Nao
    /// ha arredondamento em lugar nenhum, e o sinal e o sinal.
    /// </summary>
    public static Lado Exata(Ponto a, Ponto b, Ponto c)
    {
        var cruzado = Cruzado(a, b, c);

        return cruzado.Sign switch
        {
            > 0 => Lado.Esquerda,
            < 0 => Lado.Direita,
            _ => Lado.EmLinha,
        };
    }

    /// <summary>O determinante exato, que e o produto cruzado dos dois vetores.</summary>
    public static BigInteger Cruzado(Ponto a, Ponto b, Ponto c)
    {
        var abx = (BigInteger)b.X - a.X;
        var aby = (BigInteger)b.Y - a.Y;
        var acx = (BigInteger)c.X - a.X;
        var acy = (BigInteger)c.Y - a.Y;

        return abx * acy - aby * acx;
    }

    /// <summary>
    /// O MESMO predicado com numeros de precisao dupla, que e como quase toda
    /// biblioteca de geometria o escreve.
    ///
    /// Ele e mais rapido, ele e o que esta em todo livro, e ele erra. A medida
    /// conta em quantos casos.
    /// </summary>
    public static Lado ComVirgula(Ponto a, Ponto b, Ponto c)
    {
        var abx = (double)b.X - a.X;
        var aby = (double)b.Y - a.Y;
        var acx = (double)c.X - a.X;
        var acy = (double)c.Y - a.Y;

        var cruzado = abx * acy - aby * acx;

        return cruzado switch
        {
            > 0 => Lado.Esquerda,
            < 0 => Lado.Direita,
            _ => Lado.EmLinha,
        };
    }

    /// <summary>
    /// A MESMA conta com uma tolerancia, que e a tentativa de conserto que todo
    /// mundo faz.
    ///
    /// Ela troca um erro por outro: agora pontos que NAO estao em linha passam por
    /// colineares, e a escolha da tolerancia depende da escala dos dados, que o
    /// autor da biblioteca nao conhece.
    /// </summary>
    public static Lado ComTolerancia(Ponto a, Ponto b, Ponto c, double tolerancia = 1e-9)
    {
        var abx = (double)b.X - a.X;
        var aby = (double)b.Y - a.Y;
        var acx = (double)c.X - a.X;
        var acy = (double)c.Y - a.Y;

        var cruzado = abx * acy - aby * acx;

        if (Math.Abs(cruzado) <= tolerancia) return Lado.EmLinha;

        return cruzado > 0 ? Lado.Esquerda : Lado.Direita;
    }

    /// <summary>
    /// A SIMETRIA que o predicado precisa ter: trocar dois pontos inverte o sinal.
    ///
    /// Esta e a propriedade de que os algoritmos dependem sem nunca dizer, e e ela
    /// que a versao com virgula quebra. Se o predicado diz que C esta a esquerda de
    /// AB e que A esta a esquerda de BC, o algoritmo tem duas crencas
    /// incompativeis ao mesmo tempo.
    /// </summary>
    public static bool EhSimetrico(Func<Ponto, Ponto, Ponto, Lado> predicado, Ponto a, Ponto b, Ponto c)
    {
        ArgumentNullException.ThrowIfNull(predicado);

        var direto = predicado(a, b, c);

        return predicado(b, a, c) == Inverter(direto) &&
               predicado(a, c, b) == Inverter(direto) &&
               predicado(c, b, a) == Inverter(direto) &&
               predicado(b, c, a) == direto &&
               predicado(c, a, b) == direto;
    }

    public static Lado Inverter(Lado lado) => (Lado)(-(int)lado);

    /// <summary>
    /// O ponto esta DENTRO do triangulo, pelo predicado dado.
    ///
    /// Esta e a conta mais basica que usa o predicado, e ela mostra o outro jeito
    /// de o erro aparecer: um ponto que esta dentro e dito fora, ou o contrario.
    /// </summary>
    public static bool DentroDoTriangulo(Func<Ponto, Ponto, Ponto, Lado> predicado, Ponto a, Ponto b, Ponto c, Ponto p)
    {
        ArgumentNullException.ThrowIfNull(predicado);

        var um = predicado(a, b, p);
        var dois = predicado(b, c, p);
        var tres = predicado(c, a, p);

        var temEsquerda = um == Lado.Esquerda || dois == Lado.Esquerda || tres == Lado.Esquerda;
        var temDireita = um == Lado.Direita || dois == Lado.Direita || tres == Lado.Direita;

        return !(temEsquerda && temDireita);
    }
}
