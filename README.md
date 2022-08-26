# NEATWeapons

This project is a tool based on [UnityNEAT](https://github.com/lordjesus/UnityNEAT), that was developed in an effort to evolve 2D weapons and their projectiles to avoid the obstacles and hit the targets that exist inside a given testbed.

## Usage

To use NEATWeapons you have to download the folder and import it into your unity project. A restart of the project may be required to get rid of some errors. In it the main scene will provide examples of several testbeds that can be used to evolve and simulate weapons in them. In order for NEATWeapons to work correctly you should follow the numbered steps listed in [UnityNEAT](https://github.com/lordjesus/UnityNEAT)'s readme section. 

Some things that were added to the Optimizer class include the ability to select the generation that the user wants the evolutionary process to stop. Also the number of the simulations that an already evolved weapon will run can also be selected. Lastly, a modular list exists that can contain the number of testbeds through which the user wants the weapon to evolve. In case more than one are selected, the testbeds will transition automatically from one to the next after the number of generations set in the variable stated in the start of this paragraph. All these variable can be seen and interacted with inside Unity's inspector.

In case the user decides to change the number of input and/or output nodes of the networks, they should be careful to change the FireRunBest and FireTesting scripts to accomodate for the changes.

NEATWeapons has been used exclusively for the reason stated above and, as a result, its usage for other approaches has not been tested.
