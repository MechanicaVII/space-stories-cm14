-st-ff-number = #ffd166
-st-ff-damage = #ff6b6b
-st-ff-heal = #6fdc8c
-st-ff-fire = #ff9f43
-st-ff-xeno = #c792ea
-st-ff-build = #74b9ff
-st-ff-misc = #a0e7e5

st-fun-facts-header = [bold][color=#ffd166]Забавные факты:[/color][/bold]

st-fun-facts-survived-1 = и { $gender ->
    [female] выжила
   *[other] выжил
}! Отличная работа!
st-fun-facts-survived-2 = и { $gender ->
    [female] дожила
   *[other] дожил
} до конца раунда. Так держать!
st-fun-facts-survived-3 = и всё ещё { $gender ->
    [female] осталась
   *[other] остался
} в строю!
st-fun-facts-died-1 = до своей гибели. Хорошая работа!
st-fun-facts-died-2 = прежде чем пасть. Вечная память!
st-fun-facts-died-3 = перед смертью. Погибнуть не зря - тоже достижение!
st-fun-facts-died-to-1 = до гибели от [bold][color={ -st-ff-damage }]{ $cause }[/color][/bold]. Хорошая работа!
st-fun-facts-died-to-2 = прежде чем пасть от [bold][color={ -st-ff-damage }]{ $cause }[/color][/bold]. Вечная память!
st-fun-facts-died-to-3 = но всё же { $gender ->
    [female] погибла
   *[other] погиб
} от [bold][color={ -st-ff-damage }]{ $cause }[/color][/bold]. Месть будет сладкой!

st-fun-fact-kills = [bold]{ $name }[/bold] { $gender ->
    [female] совершила
   *[other] совершил
} [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-damage }]{ $value ->
    [one] убийство
    [few] убийства
   *[other] убийств
}[/color][/bold]
st-fun-fact-damage-taken = [bold]{ $name }[/bold] { $gender ->
    [female] получила
   *[other] получил
} [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-damage }]урона[/color][/bold]
st-fun-fact-damage-dealt = [bold]{ $name }[/bold] { $gender ->
    [female] нанесла
   *[other] нанёс
} врагам [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-damage }]урона[/color][/bold]
st-fun-fact-friendly-damage = [bold]{ $name }[/bold] { $gender ->
    [female] нанесла
   *[other] нанёс
} [color={ -st-ff-damage }]своим же союзникам[/color] [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-damage }]урона[/color][/bold]
st-fun-fact-crit-time = [bold]{ $name }[/bold] { $gender ->
    [female] провела
   *[other] провёл
} [color={ -st-ff-damage }]в критическом состоянии[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] минуту
    [few] минуты
   *[other] минут
}[/bold]
st-fun-fact-revives = [bold]{ $name }[/bold] [color={ -st-ff-heal }]{ $gender ->
    [female] реанимировала
   *[other] реанимировал
}[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] человека
    [few] человека
   *[other] человек
}[/bold]
st-fun-fact-times-revived = [bold]{ $name }[/bold] [color={ -st-ff-heal }]{ $gender ->
    [female] возвращалась
   *[other] возвращался
} с того света[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] раз
    [few] раза
   *[other] раз
}[/bold]
st-fun-fact-evolutions = [bold]{ $name }[/bold] [color={ -st-ff-xeno }]{ $gender ->
    [female] эволюционировала
   *[other] эволюционировал
}[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] раз
    [few] раза
   *[other] раз
}[/bold]
st-fun-fact-shots = [bold]{ $name }[/bold] { $gender ->
    [female] выпустила
   *[other] выпустил
} [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-misc }]{ $value ->
    [one] патрон
    [few] патрона
   *[other] патронов
}[/color][/bold] с меткостью [bold][color={ -st-ff-number }]{ $accuracy }%[/color][/bold]
st-fun-fact-healed = [bold]{ $name }[/bold] [color={ -st-ff-heal }]{ $gender ->
    [female] вылечила
   *[other] вылечил
}[/color] другим [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-heal }]урона[/color][/bold]
st-fun-fact-surgeries = [bold]{ $name }[/bold] { $gender ->
    [female] провела
   *[other] провёл
} [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-heal }]{ $value ->
    [one] операцию
    [few] операции
   *[other] операций
}[/color][/bold]
st-fun-fact-built = [bold]{ $name }[/bold] [color={ -st-ff-build }]{ $gender ->
    [female] построила
   *[other] построил
}[/color] [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-build }]{ $value ->
    [one] укрепление
    [few] укрепления
   *[other] укреплений
}[/color][/bold]
st-fun-fact-dogtags = [bold]{ $name }[/bold] { $gender ->
    [female] собрала
   *[other] собрал
} с павших [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-misc }]{ $value ->
    [one] жетон
    [few] жетона
   *[other] жетонов
}[/color][/bold]
st-fun-fact-grenades = [bold]{ $name }[/bold] { $gender ->
    [female] привела
   *[other] привёл
} в действие [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-fire }]{ $value ->
    [one] гранату
    [few] гранаты
   *[other] гранат
}[/color][/bold]
st-fun-fact-ignited = [bold]{ $name }[/bold] [color={ -st-ff-fire }]{ $gender ->
    [female] загоралась
   *[other] загорался
}[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] раз
    [few] раза
   *[other] раз
}[/bold]
st-fun-fact-eaten = [bold]{ $name }[/bold] { $gender ->
    [female] съела
   *[other] съел
} [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-misc }]{ $value ->
    [one] блюдо
    [few] блюда
   *[other] блюд
}[/color][/bold]
st-fun-fact-nested = [bold]{ $name }[/bold] { $gender ->
    [female] закрепила
   *[other] закрепил
} [color={ -st-ff-xeno }]в гнёздах[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] носителя
    [few] носителя
   *[other] носителей
}[/bold]
st-fun-fact-infected = [bold]{ $name }[/bold] [color={ -st-ff-xeno }]{ $gender ->
    [female] заразила
   *[other] заразил
}[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] носителя
    [few] носителя
   *[other] носителей
}[/bold]
st-fun-fact-resin = [bold]{ $name }[/bold] { $gender ->
    [female] выделила
   *[other] выделил
} [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-xeno }]{ $value ->
    [one] постройку
    [few] постройки
   *[other] построек
} из смолы[/color][/bold]
st-fun-fact-weeds = [bold]{ $name }[/bold] { $gender ->
    [female] посадила
   *[other] посадил
} [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-xeno }]{ $value ->
    [one] узел
    [few] узла
   *[other] узлов
} травы[/color][/bold]
st-fun-fact-acid = [bold]{ $name }[/bold] { $gender ->
    [female] применила
   *[other] применил
} [color={ -st-ff-xeno }]кислоту[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] раз
    [few] раза
   *[other] раз
}[/bold]
st-fun-fact-xeno-heals = [bold]{ $name }[/bold] [color={ -st-ff-heal }]{ $gender ->
    [female] лечила
   *[other] лечил
} сестёр[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] раз
    [few] раза
   *[other] раз
}[/bold]
st-fun-fact-knockdowns = [bold]{ $name }[/bold] [color={ -st-ff-damage }]{ $gender ->
    [female] сбивала
   *[other] сбивал
} врагов с ног[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] раз
    [few] раза
   *[other] раз
}[/bold]
st-fun-fact-rescues = [bold]{ $name }[/bold] [color={ -st-ff-heal }]{ $gender ->
    [female] освободила
   *[other] освободил
} из гнёзд[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] человека
    [few] человека
   *[other] человек
}[/bold]
st-fun-fact-deaths = [bold]{ $name }[/bold] [color={ -st-ff-damage }]{ $gender ->
    [female] погибала
   *[other] погибал
}[/color] [bold][color={ -st-ff-number }]{ $value }[/color] { $value ->
    [one] раз
    [few] раза
   *[other] раз
}[/bold] за раунд. Повторение - мать учения!
st-fun-fact-larvae = На счету [bold]{ $name }[/bold] [bold][color={ -st-ff-number }]{ $value }[/color] [color={ -st-ff-xeno }]{ $value ->
    [one] личинка
    [few] личинки
   *[other] личинок
}[/color][/bold], вылупившихся из заражённых носителей!

cmd-funfacts-desc = Показывает текущую статистику забавных фактов по всем отслеживаемым телам и игрокам.
cmd-funfacts-help = Использование: funfacts
cmd-funfacts-empty = Статистика забавных фактов пока не собрана.
