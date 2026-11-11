-- Инициализировать базовые объекты из файла
init_base_objects = function()
    if base_tech_objects == nil then
        return
    end

    if get_main_packages ~= nil then
        local packages = get_main_packages()
        for _, package in pairs(packages) do
            AddPackage(package)
        end
    end

    local objects = base_tech_objects()
    for eplanName, value in pairs(objects) do
        -- Данные для минимальной инициализации
        local name = value.name or ""
        local defaultNameEplan = value.defaultNameEplan ~= "" and value.defaultNameEplan or "TANK"
        local s88Level = value.s88Level or 0
        local basicName = value.basicName or ""
        local bindingName = value.bindingName or ""
        local isPid = value.isPID or false
        local luaModuleName = value.luaModuleName or ""
        local monitorName = value.monitorName ~= "" and value.monitorName or "TankObj"
        local defaultTechType = value.defaultTechType or 2
        local deprecated = value.deprecated or false
        local denyBindingToUnit = value.denyBindingToUnit or false
        local isSingleInstance = value.isSingleInstance or false

        -- Добавить базовый объект
        local baseObject = AddBaseObject(name, eplanName, s88Level,
            basicName, bindingName, isPid, luaModuleName, monitorName,
            deprecated, denyBindingToUnit, defaultTechType, defaultNameEplan,
            isSingleInstance)

        -- Добавить группы танков
        local objectGroups = value.objectGroups or { }
        init_objectGroups(baseObject, objectGroups)

        -- Добавить базовые операции (параметры, шаги)
        local baseOperations = value.baseOperations or { }
        init_base_operations(baseObject, baseOperations)

        -- Добавить оборудование
        local equipment = value.equipment or { }
        init_equipment(baseObject, equipment)

        -- Добавить параметры объекта как агрегата
        local aggregateParameters = value.aggregateParameters or { }
        init_aggregate_parameters(baseObject, aggregateParameters)

        -- Добавить системные параметры объекта
        local systemParameters = value.systemParams or { }
        init_system_parameters(baseObject, systemParameters)

        -- Добавить параметры объекта
        local parameters = value.parameters or { }
        init_parameters(baseObject, parameters)

        -- Добавить доп. свойства объекта
        local baseProperties = value.baseProperties or { }
        local basePropertiesObj = baseObject.BaseProperties
        init_base_properties(basePropertiesObj, baseProperties)
    end

    return 0
end

-- Инициализация оборудования объекта
init_equipment = function(object, equipment)
    for luaName, value in pairs(equipment) do
        -- Данные для добавления оборудования
        local name = value.name or ""
        local defaultValue = value.defaultValue or ""
        
        -- Добавить оборудование
        local equip_parameter = object:AddEquipment(luaName, name, defaultValue)
        if value.related ~= nil then
            init_equipment(equip_parameter, value.related)
        end
    end
end

-- Инициализация параметров объекта как агрегата
init_aggregate_parameters = function(object, aggregateParameters)
    -- Добавить активные параметры агрегата
    local activeAggregateParameters = aggregateParameters.active or { }
    init_aggregate_active_parameters(object, activeAggregateParameters)
    -- Добавить булевые параметры агрегата
    local boolAggregateParameters = aggregateParameters.bool or { }
    init_active_bool_parameters(object, boolAggregateParameters)
    -- Добавить главный параметр агрегата
    local mainAggregateParameter = aggregateParameters.main or { }
    init_main_aggregate_parameter(object, mainAggregateParameter)
end

-- Инициализация базовых операций объекта
init_base_operations = function(object, operations)
    for luaName, value in pairs(operations) do
        -- Данные для минимальной инициализации операции
        local name = value.name or ""
        local defaultPosition = value.defaultPosition or 0

        -- Добавить базовую операцию
        local operation = object:AddBaseOperation(luaName, name, defaultPosition)

        -- Добавить параметры базовой операции
        local params = value.params or { }
        init_operation_parameters(operation, params)

        -- Добавить состояния и их шаги
        local states = value.state or { }
        init_operation_states(operation, states)
    end
end

-- Инициализация параметров базовой операции
init_operation_parameters = function(operation, params)
    -- Добавить активные параметры операции
    local activeParameters = params.active or { }
    init_active_parameters(operation, activeParameters)

    -- Добавить булевы параметра операции
    local activeBoolParameters = params.bool or { }
    init_active_bool_parameters(operation, activeBoolParameters)

    -- Добавить групповые параметры
    local group = params.group or {}
    init_group_parameters(operation, group)

    -- Добавить float параметры операции
    local floatParameters = params.float or {}
    init_float_parameters(operation, floatParameters);
end

---Инициализация групповых параметров
---@param object table объект для инициализация параметра
---@param groups table описание групповых параметров
init_group_parameters = function (object, groups)
    for key, group in pairs(groups) do
        local luaName = group.luaName or key
        if type(luaName) == "number" then return end

        local name = group.name or ""
        local main = group.main or false
        local ignoreCompoundName = group.ignoreCompoundName or false

        local group_parameter = object:AddGroupParameter(luaName, name, main, ignoreCompoundName)
        init_operation_parameters(group_parameter, group)
    end
end

-- Инициализация активных параметров агрегата
-- object - базовый объект
-- activeParameters - список параметров
init_aggregate_active_parameters = function (object, activeParameters)
    for key, value in pairs(activeParameters) do
        -- Данные для добавления параметра
        local luaName = value.luaName or key
        if type(luaName) == "number" then return end

        local name = value.name or ""
        local displayObjects = value.displayObjects or { }
        local defaultValue = value.defaultValue or ""

        if type(defaultValue) == "table" then
            -- Добавить активный параметр агрегата с float-параметром по умолчанию
            local parameter = object:AddActiveAggregateParameter(luaName, name)
            parameter:SetFloatParameter(defaultValue.luaName or "", defaultValue.name or "",
                defaultValue.defaultValue or 0, defaultValue.meter or "")
        else
            -- Добавить активный параметр
            local parameter = object:AddActiveParameter(luaName, name, defaultValue)
        
            for _, value in pairs(displayObjects) do
                parameter:AddDisplayObject(value)
            end
        end
    end
end

-- Инициализация активных параметров
-- object - базовая операция или базовый объект
init_active_parameters = function(object, activeParameters)
	for key, value in pairs(activeParameters) do
        -- Данные для добавления параметра
        local luaName = value.luaName or key
        if type(luaName) == "number" then return end

        local name = value.name or ""
        local defaultValue = value.defaultValue or ""
        local displayObjects = value.displayObjects or { }

        -- Добавить активный параметр
        local parameter = object:AddActiveParameter(luaName, name, defaultValue)

       for showProperty, value in pairs(displayObjects) do
            parameter:AddDisplayObject(value)
        end

    end
end

-- Инициализация булевых параметров
-- object - базовая операция или базовый объект
init_active_bool_parameters = function(object, activeBoolParameters)
	for key, value in pairs(activeBoolParameters) do
        -- Данные для добавления параметра
        local luaName = value.luaName or key
        if type(luaName) == "number" then return end

        local name = value.name or ""
        local defaultValue = value.defaultValue or ""

        -- Добавить булевый параметр
        object:AddActiveBoolParameter(luaName, name, defaultValue)
    end
end

-- Инициализация главного параметра агрегата
init_main_aggregate_parameter = function(object, tableWithParameter)
    for key, value in pairs(tableWithParameter) do
        local luaName = value.luaName or key
        if type(luaName) == "number" then return end
        
        local name = value.name or ""
        local defaultValue = value.defaultValue or ""
        object:AddMainAggregateParameter(luaName, name, defaultValue)
    end
end

-- Инициализация состояний базовой операции
init_operation_states = function(operation, states)
    for stateLuaName, state in pairs(states) do
        -- Базовые шаги состояния
        local steps = state.steps or { }

        -- Инициализировать базовые шаги состояния
        init_operation_steps(operation, steps, stateLuaName)
    end
end

-- Инициализация шагов базовой операции в состоянии
init_operation_steps = function(operation, steps, stateLuaName)
    for luaName, value in pairs(steps) do
        -- Данные для добавления базового шага
        local name = value.name or ""
        local defaultPosition = value.defaultPosition or 0

        -- Добавить базовый шаг для операции
        operation:AddStep(stateLuaName, luaName, name, defaultPosition)
    end
end

-- Инициализация групп танков базового объекта
init_objectGroups = function(object, objectGroups)
    for luaName, value in pairs(objectGroups) do
        local name = value.name or ""
        local allowedObjects = value.allowedObjects or ""
        
        object:AddObjectGroup(luaName, name, allowedObjects)
    end
end

-- Инициализация системных параметров базового объекта
init_system_parameters = function(object, systemParameters)
    for index, parameter in ipairs(systemParameters) do
        local name = parameter.name or ""
        local defaultValue = parameter.defaultValue or 0
        local meter = parameter.meter or ""
        local nameLua = parameter.nameLua or ""

        object:AddSystemParameter(nameLua, name, defaultValue, meter)
    end
end

-- Инициализация параметров базового объекта
init_parameters = function(object, parameters)
    local float = parameters.float or {}
    local float_runtime = parameters.float_runtime or {}

    -- Если таблицы uint, float пусты - старый вариант записи
    if (#float == 0 and #float_runtime == 0) then
        init_float_parameters(object, parameters)
    end

    init_float_parameters(object, float)
    init_float_runtime_parameters(object, float_runtime)
end

-- Инициализация float параметров базового объекта
init_float_parameters = function(object, parameters)
    for key, parameter in ipairs(parameters) do
        local name = parameter.name or ""
        local luaName = parameter.luaName or ""
        local meter = parameter.meter or ""
        local value = parameter.value or 0

        object:AddFloatParameter(luaName, name, value, meter)
    end
end

-- Инициализация float runtime параметров базового объекта
init_float_runtime_parameters = function (object, parameters)
	for key, parameter in ipairs(parameters) do
        local name = parameter.name or ""
        local luaName = parameter.luaName or ""
        local meter = parameter.meter or ""

        object:AddFloatRuntimeParameter(luaName, name, meter)
    end
end

-- Инициализация доп. свойств базового объекта
init_base_properties = function(object, properties)
    -- Добавить активные свойства объекта
    local activeParameters = properties.active or { }
    init_active_parameters(object, activeParameters)

    -- Добавить булевые доп. свойства объекта
    local activeBoolParameters = properties.bool or { }
    init_active_bool_parameters(object, activeBoolParameters)
end
