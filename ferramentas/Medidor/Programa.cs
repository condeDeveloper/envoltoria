using Conde.Envoltoria;

var pedido = args.Length > 0 ? args[0] : "tudo";
var tudo = pedido == "tudo";

if (tudo || pedido == "juiz") Juizes();
if (tudo || pedido == "predicado") Predicados();
if (tudo || pedido == "estrago") Estragos();
if (tudo || pedido == "custo") Custos();

static void Juizes()
{
    Console.WriteLine();
    Console.WriteLine("  O JUIZ: OS TRES ALGORITMOS CONTRA A DEFINICAO (predicado exato)");
    Console.WriteLine();
    Console.WriteLine("  conjunto            algoritmo               pontos   vertices   convexa   contem tudo   bate com a definicao");

    var anterior = "";
    foreach (var l in Medidas.Juiz())
    {
        if (l.Conjunto != anterior) { Console.WriteLine(); anterior = l.Conjunto; }

        Console.WriteLine($"  {l.Conjunto,-19} {l.Algoritmo,-22} {l.Pontos,7} {l.Vertices,10} {Marca(l.Convexa),9} {Marca(l.Contem),13} {Marca(l.Bate),22}");
    }

    Console.WriteLine();
}

static void Predicados()
{
    Console.WriteLine();
    Console.WriteLine("  O PREDICADO COM VIRGULA CONTRA O EXATO, EM TODOS OS TRIOS");
    Console.WriteLine();
    Console.WriteLine("  conjunto                 trios   errou com virgula   errou com tolerancia   se contradiz");

    foreach (var l in Medidas.Predicados())
        Console.WriteLine($"  {l.Conjunto,-19} {l.Trios,10:N0} {l.ErrouComVirgula,19:N0} {l.ErrouComTolerancia,22:N0} {l.AssimetricoComVirgula,14:N0}");

    Console.WriteLine();
    Console.WriteLine("  a ultima coluna e a pior: o predicado diz coisas incompativeis sobre os");
    Console.WriteLine("  mesmos tres pontos, dependendo so da ordem em que eles chegaram");
    Console.WriteLine();
}

static void Estragos()
{
    Console.WriteLine();
    Console.WriteLine("  O MESMO ALGORITMO COM O PREDICADO COM VIRGULA");
    Console.WriteLine();
    Console.WriteLine("  conjunto            algoritmo               vertices (exato)   vertices (virgula)   convexa   contem tudo");

    var anterior = "";
    foreach (var l in Medidas.Estragos())
    {
        if (l.Conjunto != anterior) { Console.WriteLine(); anterior = l.Conjunto; }

        Console.WriteLine($"  {l.Conjunto,-19} {l.Algoritmo,-22} {l.VerticesComExato,16} {l.VerticesComVirgula,20} {Marca(l.ConvexaComVirgula),9} {Marca(l.ContemComVirgula),13}");
    }

    Console.WriteLine();
}

static void Custos()
{
    Console.WriteLine();
    Console.WriteLine("  O CUSTO EM CHAMADAS DO PREDICADO (2.000 pontos)");
    Console.WriteLine();
    Console.WriteLine("  conjunto            algoritmo               pontos   vertices      chamadas   por ponto");

    var anterior = "";
    foreach (var l in Medidas.Custos())
    {
        if (l.Conjunto != anterior) { Console.WriteLine(); anterior = l.Conjunto; }

        Console.WriteLine($"  {l.Conjunto,-19} {l.Algoritmo,-22} {l.Pontos,7:N0} {l.Vertices,10:N0} {l.Chamadas,13:N0} {l.PorPonto,11:F1}");
    }

    Console.WriteLine();
    Console.WriteLine("  O CRESCIMENTO NO CIRCULO, ONDE TODOS OS PONTOS SAO VERTICES");
    Console.WriteLine();
    Console.WriteLine("  pontos   vertices   cadeia monotona   embrulho de presente");

    foreach (var l in Medidas.Crescimentos())
        Console.WriteLine($"  {l.Pontos,6:N0} {l.Vertices,10:N0} {l.Monotona,17:N0} {l.Embrulho,22:N0}");

    Console.WriteLine();
}

static string Marca(bool valor) => valor ? "sim" : "NAO";
