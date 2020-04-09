Automation Anywhere - Process Automation Implementation
=======================================================

Aangezien er geen optie is om het uitgewerkt proces in Automation Anywhere te exporteren zal het beschreven worden aan de hand van foto's.

Flow
----

<p align="center">
  <img src="./Assets/flow_1.png" />
  <img src="./Assets/flow_2.png" />
  <img src="./Assets/flow_3.png" />
  <img src="./Assets/flow_6.png" />
  <br />
  <img src="./Assets/flow_7.png" />
  <img src="./Assets/flow_5.png" />
</p>

### Gebruikte variabelen

![](./Assets/variables.png)

### Verschillende componenten

#### Prompt Folder

Vraagt aan de gebruiker om een folder te kiezen met de bestanden in en slaat het pad op in de variabele folderPath. ![](./Assets/prompt_folder.png)

#### Bouw het filepath op (de loop)

Er wordt gelooped over elke file in de folder met pad: folderPath. Het resultaat wordt in een dictionary gestoken. Nadien wordt in meerdere stappen de fileName en extension uit de dictionary gehaald, samengezet met het folder pad om zo het volledig pad naar de file op te bouwen. Deze paden worden toegevoegd aan een array.

![](./Assets/loop.png)

![](./Assets/get_fileName.png)

![](./Assets/get_extension.png)

![](./Assets/create_full_path.png)

![](./Assets/add_path_to_array.png)

#### Custom activity

Hierin wordt de custom activity uitgevoerd. Deze zit in een try/catch om een eventuele fout op te vangen en weg te schrijven naar een log file.

![](./Assets/catch_block.png)

![](./Assets/write_to_file_fail_custom_activity.png)

#### Opbouw uncertainEntities

Met het resultaat van de custom activity kan gekeken worden of er entities zijn die onder de threshold zitten. Als dat het geval is, dan worden deze toegevoegd aan de array uncertainEntities.

![](./Assets/loop_uncertainEntities.png)

![](./Assets/assign_confidence.png)

![](./Assets/assign_threshold.png)

![](./Assets/if_confidence.png)

![](./Assets/assign_entityName.png)

![](./Assets/add_to_list_uncertainEntities.png)

![](./Assets/join_uncertainEntities.png)

#### Versturen mail (if/else)

Als de array uncertainEntities niet leeg is, dan zal een mail verstuurd worden waarin de entities worden verstuurd. Nadien wordt er gelogd dat het versturen van de mail succesvol was. Als er geen mail verstuurd moet worden, wordt enkel gelogd dat alles succesvol was. Dit hele proces zit in een try/catch om een mogelijke error op te vangen. Als dit het geval is wordt ook deze error weggeschreven naar het logbestand.

![](./Assets/if_else.png)

![](./Assets/send_mail_1.png) ![](./Assets/send_mail_2.png)

![](./Assets/write_to_file_after_mail.png)

![](./Assets/write_to_file_without_mail.png)

![](./Assets/write_to_file_error_mail.png)
