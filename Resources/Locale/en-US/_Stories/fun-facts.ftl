-st-ff-number = #ffd166
-st-ff-damage = #ff6b6b
-st-ff-heal = #6fdc8c
-st-ff-fire = #ff9f43
-st-ff-xeno = #c792ea
-st-ff-build = #74b9ff
-st-ff-misc = #a0e7e5

st-fun-facts-header = [bold][color=#ffd166]Fun facts:[/color][/bold]

st-fun-facts-survived-1 = and survived! Great work!
st-fun-facts-survived-2 = and made it to the end of the round. Keep it up!
st-fun-facts-survived-3 = and is still standing!
st-fun-facts-died-1 = before dying. Good work!
st-fun-facts-died-2 = before falling. Rest in peace!
st-fun-facts-died-3 = before their death. Not a death in vain!
st-fun-facts-died-to-1 = before dying to [bold][color={ -st-ff-damage }]{ $cause }[/color][/bold]. Good work!
st-fun-facts-died-to-2 = before falling to [bold][color={ -st-ff-damage }]{ $cause }[/color][/bold]. Rest in peace!
st-fun-facts-died-to-3 = but was still taken down by [bold][color={ -st-ff-damage }]{ $cause }[/color][/bold]. Revenge will be sweet!

st-fun-fact-kills = [bold]{ $name }[/bold] earned [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-damage }]{ $value ->
    [one] kill
   *[other] kills
}[/color][/bold]
st-fun-fact-damage-taken = [bold]{ $name }[/bold] took [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-damage }]damage[/color][/bold]
st-fun-fact-damage-dealt = [bold]{ $name }[/bold] dealt [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-damage }]damage[/color][/bold] to enemies
st-fun-fact-friendly-damage = [bold]{ $name }[/bold] dealt [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-damage }]damage[/color][/bold] to [color={ -st-ff-damage }]their own allies[/color]
st-fun-fact-crit-time = [bold]{ $name }[/bold] spent [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] minute
   *[other] minutes
}[/bold] [color={ -st-ff-damage }]in critical condition[/color]
st-fun-fact-revives = [bold]{ $name }[/bold] [color={ -st-ff-heal }]revived[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] person
   *[other] people
}[/bold]
st-fun-fact-times-revived = [bold]{ $name }[/bold] was [color={ -st-ff-heal }]brought back from the dead[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] time
   *[other] times
}[/bold]
st-fun-fact-evolutions = [bold]{ $name }[/bold] [color={ -st-ff-xeno }]evolved[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] time
   *[other] times
}[/bold]
st-fun-fact-shots = [bold]{ $name }[/bold] fired [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-misc }]{ $value ->
    [one] round
   *[other] rounds
}[/color][/bold] with [bold][color={ -st-ff-number }]{ $accuracy }%[/color][/bold] accuracy
st-fun-fact-healed = [bold]{ $name }[/bold] [color={ -st-ff-heal }]healed[/color] [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-heal }]damage[/color][/bold] on others
st-fun-fact-surgeries = [bold]{ $name }[/bold] performed [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-heal }]{ $value ->
    [one] surgery
   *[other] surgeries
}[/color][/bold]
st-fun-fact-built = [bold]{ $name }[/bold] [color={ -st-ff-build }]built[/color] [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-build }]{ $value ->
    [one] structure
   *[other] structures
}[/color][/bold]
st-fun-fact-dogtags = [bold]{ $name }[/bold] collected [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-misc }]{ $value ->
    [one] dogtag
   *[other] dogtags
}[/color][/bold] from the fallen
st-fun-fact-grenades = [bold]{ $name }[/bold] primed [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-fire }]{ $value ->
    [one] explosive
   *[other] explosives
}[/color][/bold]
st-fun-fact-ignited = [bold]{ $name }[/bold] [color={ -st-ff-fire }]caught fire[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] time
   *[other] times
}[/bold]
st-fun-fact-eaten = [bold]{ $name }[/bold] ate [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-misc }]{ $value ->
    [one] meal
   *[other] meals
}[/color][/bold]
st-fun-fact-nested = [bold]{ $name }[/bold] secured [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] host
   *[other] hosts
}[/bold] [color={ -st-ff-xeno }]to nests[/color]
st-fun-fact-infected = [bold]{ $name }[/bold] [color={ -st-ff-xeno }]infected[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] host
   *[other] hosts
}[/bold]
st-fun-fact-resin = [bold]{ $name }[/bold] secreted [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-xeno }]resin { $value ->
    [one] structure
   *[other] structures
}[/color][/bold]
st-fun-fact-weeds = [bold]{ $name }[/bold] planted [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-xeno }]weed { $value ->
    [one] node
   *[other] nodes
}[/color][/bold]
st-fun-fact-acid = [bold]{ $name }[/bold] applied [color={ -st-ff-xeno }]acid[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] time
   *[other] times
}[/bold]
st-fun-fact-xeno-heals = [bold]{ $name }[/bold] [color={ -st-ff-heal }]healed sisters[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] time
   *[other] times
}[/bold]
st-fun-fact-knockdowns = [bold]{ $name }[/bold] [color={ -st-ff-damage }]knocked down enemies[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] time
   *[other] times
}[/bold]
st-fun-fact-rescues = [bold]{ $name }[/bold] [color={ -st-ff-heal }]freed[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] person
   *[other] people
}[/bold] [color={ -st-ff-heal }]from nests[/color]
st-fun-fact-deaths = [bold]{ $name }[/bold] [color={ -st-ff-damage }]died[/color] [bold][color={ -st-ff-number }]{ $value }[/color] times[/bold] this round. Practice makes perfect!
st-fun-fact-larvae = [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-xeno }]larvae[/color][/bold] burst from hosts infected by [bold]{ $name }[/bold]!

cmd-funfacts-desc = Shows the current fun facts statistics of every tracked body and player.
cmd-funfacts-help = Usage: funfacts
cmd-funfacts-empty = No fun facts statistics have been collected yet.
