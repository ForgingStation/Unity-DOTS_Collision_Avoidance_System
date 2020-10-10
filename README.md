# Unity-DOTS_Collision_Avoidance_System
Source code for Unity DOTS based Collision Avoidance System - https://youtu.be/pGCVO1FV7PU

UnitV2_Component - The componentData struct which holds the necessary data for every unit.
Unit_Spawner_Base - A Monobehaviour class spawning entity with necessary components. Any code block with a comment - Debug - can be commented to increase performance.
Unit_Movement_System - A system base operating on UnitV2_Component, responsible for movement and collision avoidance. Two methods demonstrated in this video exist as separate foreach loops on entities.
Unit_Buffer - A dynamic buffer holding two locations between which units traverse.
