# IEA

Island Evolutionary Algorithms models are popular extension of classical EAs. The very idea behind IEA is to divide the population of candidate solutions into several subpopulations, each evolving independently on different islands. Not only is it not required for all islands to posses the same Mutation and Crossover operators but also there is no need to apply the same Fitness Function.
As each Island evolves in different manner, they do not „blend” their solutions. Knowledge sharing is performed via migration operator. In every epoch, it is not possible for all solutions to cross with every other one.

This repo consist of a project for Distributed Systems classes at AGH UST in Cracow. Project is a mere PoC of industrial standard of HPC for real-life problem of assigning students to their groups.

Detailed problem decription can be found in presentation in *Docs* directory.

Deployment process is thoroughly investigated in *deployment* directory README file.

Credits to [Dominik](https://github.com/dominik-air) for prolific collaboration.

Should you have any questions please contact me directly:
igor@ratajczyk.eu
