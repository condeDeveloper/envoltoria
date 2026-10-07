# envoltoria

Três algoritmos de **envoltória convexa** do zero em C# e .NET 8: cadeia
monótona, embrulho de presente e varredura de Graham. O juiz é a **definição**,
escrita como força bruta: um par de pontos é uma aresta quando todos os outros
ficam do mesmo lado dele.

E a pergunta do repositório não é qual dos três é mais rápido. É esta:

```
$ dotnet medidor.dll predicado

  conjunto                 trios   errou com virgula   errou com tolerancia   se contradiz
  sorteados              205,320                   0                      0              0
  na grade               205,320                   0                      0              0
  no circulo             205,320                   0                      0              0
  com repetidos            2,730                   0                      0              0
  quase em linha         205,320                   0                      0              0
  coordenadas grandes    205,320                   0                      0              0
  Fibonacci              205,320             131,936                131,936         75,732
```

Todo algoritmo de geometria se apoia numa pergunta só: **este ponto está de que
lado desta reta?** Com coordenadas inteiras, a resposta é o sinal de um
determinante e ela é exata. Com coordenadas com vírgula, ela é a subtração de
dois produtos quase iguais, que é exatamente o caso em que o arredondamento mais
machuca.

A última coluna é a pior das três. Ela não diz que o predicado é impreciso: diz
que ele é **incoerente**. Nos mesmos três pontos, ele responde "C está à
esquerda de AB" e "A está à esquerda de CB" ao mesmo tempo, e qual das duas você
recebe depende só da ordem em que os pontos chegaram.

## O que acontece com um algoritmo em cima de um predicado que se contradiz

```
$ dotnet medidor.dll estrago

  conjunto            algoritmo               vertices (exato)   vertices (virgula)   convexa   contem tudo

  no circulo          cadeia monotona                      300                  300       sim           sim
  no circulo          embrulho de presente                 300                  300       sim           sim
  no circulo          varredura de Graham                  300                  300       sim           sim

  Fibonacci           cadeia monotona                        3                    4       NAO           NAO
  Fibonacci           embrulho de presente                   3                    4       NAO           NAO
  Fibonacci           varredura de Graham                    3                    3       sim           NAO
```

Ele não estoura. Não devolve vazio. Não lança exceção. Devolve uma lista de
pontos com o tamanho plausível, que **não é um polígono convexo** e **não contém
os pontos de entrada**. Nada avisa.

E repare na terceira linha do Fibonacci: a varredura de Graham devolve uma coisa
que até é convexa, com o número certo de vértices, e ainda assim deixa pontos de
fora. As duas conferências do juiz são independentes de propósito, porque
nenhuma delas sozinha caracteriza a resposta certa.

## O custo, em chamadas do predicado

```
$ dotnet medidor.dll custo

  conjunto            algoritmo               pontos   vertices      chamadas   por ponto

  sorteados           cadeia monotona          2,000         21         7,968         4.0
  sorteados           embrulho de presente     2,000         21        41,979        21.0
  sorteados           varredura de Graham      2,000         21        29,090        14.5

  no circulo          cadeia monotona          2,000      2,000         5,994         3.0
  no circulo          embrulho de presente     2,000      2,000     3,998,000      1999.0
  no circulo          varredura de Graham      2,000      2,000        27,095        13.5
```

O embrulho de presente paga o número de pontos vezes o número de **vértices da
resposta**, e esse é o único dos três cujo custo depende da resposta e não da
entrada. Com dois mil pontos sorteados, onde a envoltória tem vinte e um
vértices, ele faz vinte e uma chamadas por ponto. Com os mesmos dois mil pontos
num círculo, onde todos são vértices, ele faz mil novecentas e noventa e nove.

```
  pontos   vertices   cadeia monotona   embrulho de presente
     100        100               294                  9,900
     200        200               594                 39,800
     400        400             1,194                159,600
     800        800             2,394                639,200
   1,600      1,600             4,794              2,558,400
```

Dobrar os pontos dobra um lado e quadruplica o outro, na mesma tabela.

## Por que nenhum deles usa ponto flutuante

Nenhum dos três algoritmos sabe o que é um ponto. Cada um recebe o predicado de
orientação como argumento, e o repositório tem quatro:

- `Exata`: o determinante em `BigInteger`, sem limite de tamanho;
- `Cruzado`: o mesmo determinante devolvido inteiro, para conferir;
- `ComVirgula`: o mesmo determinante em `double`;
- `ComTolerancia`: o `double` com uma margem de erro, que é o conserto que todo
  mundo tenta primeiro.

É isso que torna a comparação honesta: a diferença entre as tabelas não é de
algoritmo, nem de linguagem, nem de biblioteca. Os mesmos três algoritmos, nos
mesmos pontos, trocando uma função de três linhas.

## Como rodar

```
dotnet test testes/Envoltoria.Testes/Envoltoria.Testes.csproj
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

Os comandos do medidor são `juiz`, `predicado`, `estrago`, `custo` e `tudo`.

A integração contínua roda nos três sistemas, e isso importa aqui mais que de
costume: o determinante em precisão dupla depende da ordem de avaliação dos
produtos, e "errou em 131.936 de 205.320 trios" só é um número se for o mesmo
número no Linux, no Windows e no macOS.

## O que as medidas me corrigiram

**Achar um conjunto que derruba o predicado com vírgula me custou quatro
tentativas, e todas as quatro deram uma tabela de zeros.** Coordenadas sorteadas
até um milhão: zero. Até dez milhões: zero. Pontos "quase em linha" com
coordenadas até um bilhão: zero. Pontos sorteados até dois bilhões: zero.

A conta explica. Com coordenadas de até dez milhões, os dois produtos do
determinante ficam na ordem de 10^14, que é **menor que 2^53**: a precisão dupla
representa os dois exatamente, e a subtração de dois inteiros exatos também é
exata. O arredondamento só começa acima de nove mil trilhões.

E o que derruba não é a coordenada ser grande. É o **determinante ser menor que
o erro**. A razão entre dois números de Fibonacci seguidos é a melhor
aproximação racional possível da razão áurea, então os pontos `(F(i), F(i+1))`
ficam colados numa reta de um jeito que nenhuma outra escolha de inteiros
consegue: o determinante de três deles sai na casa das unidades, com os produtos
na casa de 10^18.

Foi aí que o bug apareceu. E essa dificuldade **é** o achado: um bug que não
aparece em teste nenhum com dado de tamanho normal é pior que um bug que
aparece sempre.

**A tolerância não conserta nada: ela troca um erro por outro.** Errou em 131.936
trios sem tolerância e em 131.936 com tolerância, no mesmo conjunto. A margem de
erro faz o predicado dizer "em linha" onde ele dizia "à esquerda", e isso não é
mais certo, é só diferente. Pior: a tolerância não é transitiva, então ela cria
uma classe nova de incoerência enquanto apaga a antiga.

**O juiz estava errado antes dos algoritmos.** A primeira versão da força bruta
acrescentava os **dois** pontos do par que funcionava, e isso punha na
envoltória os pontos do **meio** de uma aresta: com p, m e q alinhados, o par
(p, m) deixa todo o resto de um lado só, e o m entrava junto. Nos conjuntos com
pontos alinhados, o juiz passou a discordar dos três algoritmos ao mesmo tempo,
e três implementações independentes concordando entre si contra o juiz é
exatamente o sinal de que o errado é ele.

**E o embrulho de presente não é uma curiosidade histórica.** Eu tinha escrito
um teste afirmando que a cadeia monótona é sempre a mais barata das três em
chamadas do predicado, e ele falhou nos sessenta pontos de Fibonacci: 177
chamadas do embrulho contra 202 da cadeia. A envoltória ali tem três vértices, e
com a resposta minúscula o custo dele cai abaixo do custo de ordenar. Ele é a
escolha certa quando a envoltória é pequena, e envoltória pequena é o caso
comum.

## Licença

MIT.
