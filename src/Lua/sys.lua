function init()
    if init_tech_objects_modes == nil then
        return
    end

    local generic_objects = nil;
    if init_generic_tech_objects ~= nil then
        generic_objects = init_generic_tech_objects();
    end
    local objects = init_tech_objects_modes();


    -- инициализация типовых объектов
    local initialized_generic_objects = init_objects(generic_objects, true)
    -- инициализация тех. объектов
    local initialized_objects = init_objects(objects, false)

    -- инициализация данных тех. объектов
    init_objects_data(objects, initialized_objects)
    -- инициализация данных типовых объектов
    init_objects_data(generic_objects, initialized_generic_objects)

    return 0
end

function init_objects(objects, is_generic)
    if objects == nil then
        return
    end

    local initialized_objects = { }
	for number, value in ipairs(objects) do
		-- Проверка пустоты таблицы для импортируемых объектов.
        -- Экспорт выполняется так, что бы пустые элементы таблицы имели длину 
        -- 1,а при импорте это игнорируется. Сохранение таблиц (файлов проекта)
        -- задает длину всех таблиц равную 0.
        if(#value == 0) then
            -- Глобальный номер объекта
            local global_number       = number -- Для импорта в редактор
		    local object_n            = value.n or 1
		    local object_name         = value.name or "Object"
		    local object_tech_type    = value.tech_type or 1
		    local object_name_eplan   = value.name_eplan or "TANK"
		    local object_name_BC      = value.name_BC or "TankObj"
		    local cooper_param_number = value.cooper_param_number or -1
		    local base_tech_object	  = value.base_tech_object or "" 

            -- Привязанные агрегаты (два формата, в зависимости от источника):
            -- attached_objects      — глобальные индексы; хранится в main.objects.lua
            -- attached_objects_refs — переносимые ссылки base:n;
            --                         только в файлах импорта/экспорта объектов
		    local attached_objects      = value.attached_objects or ""
            local attached_objects_refs = value.attached_objects_refs or ""
            local generic_tech_object_number = value.generic_tech_object or -1;

		    local obj = ADD_TECH_OBJECT(global_number, object_n, object_name,
			    object_tech_type, object_name_eplan, cooper_param_number,
			    object_name_BC, base_tech_object, attached_objects,
                attached_objects_refs, generic_tech_object_number, is_generic)
		    initialized_objects[number] = obj
        end
	end
    return initialized_objects
end

function init_objects_data(objects, initialized_objects)
    if objects == nil then
        return
    end

    for fields, value in ipairs(objects) do
        -- Аналогично с предыдущим циклом.
        if (#value == 0) then
            local obj = initialized_objects[fields]

            --Операции (инциализация всегда перед параметрами)
            for fields, value in ipairs(value.modes) do
                local mode_name = value.name or "Операция ??"
                local mode_base_operation = value.base_operation or ""

                --Доп. свойства по операции
                local mode_base_operation_props = {}
                if value.props then
                    for fields, value in pairs(value.props) do
                        mode_base_operation_props[fields] = value 
                    end
                end

                local mode = obj:AddMode(mode_name, mode_base_operation, 
                    mode_base_operation_props)

                --Состояния
                if value.states then
                    for fields, value in pairs(value.states) do
                        local state_n = fields
                        proc_operation_devices(value, mode, state_n)
                    end
                end
            end

            --Параметры
            proc_params(value.par_float, "par_float", obj)
            proc_params(value.par_uint, "par_uint", obj)
            proc_params(value.rt_par_float, "rt_par_float", obj)
            proc_params(value.rt_par_uint, "rt_par_uint", obj)
            obj:GetParamsManager():CompleteInit()

            -- Системные параметры
            proc_system_params(value.system_parameters, obj)

            -- Свойства объекта
            proc_object_properties(value.properties, obj)

            --Оборудование (всегда после параметров)
            if value.equipment then
                for field, value in pairs(value.equipment) do
                    obj:SetEquipment(field, value)
                end
            end

            --Группы объектов
            if value.tank_groups then
                for luaName, value in pairs(value.tank_groups) do
                    obj:SetGroupObject(luaName, value)
                end
            end
        end
    end
end

function proc_params(par, par_name, obj)
    if type(par) == "table" then
        for fields, value in ipairs(par) do
            local param = obj:GetParamsManager():AddParam(par_name,
                value.name or "Параметр", value.value or 0,
                value.meter or "шт.", value.nameLua or "")
            if value.oper then param:SetOperationN(value.oper) end
        end
    end
end

function proc_system_params(par, obj)
    if type (par) == "table" then
        for luaName, param in pairs(par) do
            local parValue = param.value
            obj:SetSystemParameter(luaName, parValue)
        end
    end
end

function proc_object_properties(par, obj)
    if type (par) == "table" then
        for luaName, value in pairs(par) do
            obj:SetBaseProperty(luaName, value)
        end
    end
end

function proc_operation_devices(value, mode, state_n)
    if not mode[state_n] then
        return
    end
    local mainStep = mode[state_n][-1]
    proc_actions(mainStep, value)

    local state = mode[state_n]
    if value.runPoint == true then
        state:SetRunPoint()
    end

    if value.steps then
        for fields, value in ipairs(value.steps) do
            mode:AddStep(state_n, value.name or "Шаг ??", value.baseStep or "")
            local step_n = fields - 1
            local step = mode[state_n][step_n]
            proc_actions(step, value)

            local next_step_n = value.next_step_n or 0
            local time_param_n = value.time_param_n or 0
            local step_max_duration_par_n = value.step_max_duration_par_n or -1
            local attached_object = value.attached_object or -1
            if time_param_n > 0 or step_max_duration_par_n then
                step:SetPar(time_param_n, next_step_n, step_max_duration_par_n)
            end
            step:SetAttachedObject(attached_object)
        end
    end
end

function proc_actions(step, value, is_runtime_step)
    proc(step, value, "checked_devices")
    proc(step, value, "opened_devices")
    proc_action_custom_groups(step, value, "delay_opened_devices")
    proc(step, value, "opened_reverse_devices")
    proc(step, value, "closed_devices")
    proc_action_custom_groups(step, value, "delay_closed_devices")
    proc_groups(step, value, "opened_upper_seat_v")
    proc_groups(step, value, "opened_lower_seat_v")
    proc(step, value, "required_FB")
    proc_action_custom_groups(step, value, "DI_DO")
    proc_groups(step, value, "inverted_DI_DO")
    proc_groups(step, value, "AI_AO")
    proc_wash_data(step, value)
    proc_action_custom_groups(step, value, "enable_step_by_signal")
    proc_action_custom_groups(step, value, "jump_if")
end

function proc(step, actions, action_name, group_number, sub_action_name)
    if (group_number == nil) then
        group_number = 0
    end

    if (sub_action_name == nil) then
        sub_action_name = ""
    end

    if (actions == nil) then
        return
    end

    for current_action_name, devices in pairs (actions) do
        if (current_action_name == action_name and devices) then
            for _, dev_name in pairs (devices) do
                add_dev(step, action_name, dev_name, group_number,
                    sub_action_name)
            end  
        end
    end
end

function proc_groups(step, actions, action_name)
    if (actions == nil) then
        return
    end

    for current_action_name, groups in pairs (actions) do
        if (current_action_name == action_name and groups) then
            local group_n = 0
            for _, group in pairs(groups) do
                for _, dev_name in pairs(group) do
                    add_dev(step, action_name, dev_name, group_n, "")
                end
                group_n = group_n + 1
            end 
        end
    end  
end

function proc_wash_data(step, value)
    local devices_data = value.devices_data -- Новая функциональность.
    local wash_data = value.wash_data -- Старая функциональность.

    if devices_data then
        wash_data = devices_data
    end

    --Группа устройств, управляемых по ОС с выдачей сигнала
    if wash_data then
        if (wash_data.DI or
            wash_data.DO or
            wash_data.devices or
            wash_data.deactivate_devices or
            wash_data.rev_devices or
            wash_data.pump_freq) then

            local group_number = 0
            proc_wash_group_data(step, wash_data, group_number)
        else -- Process new version
            local group_number = 0
            for _, wash_group in pairs(wash_data) do
                proc_wash_group_data(step, wash_group, group_number)
                group_number = group_number + 1
            end
        end
    end
end

function proc_wash_group_data(step, wash_data, group_number)
    local action_name = "devices_data"
    for sub_action_name, devices in pairs(wash_data) do                                                                
        if (type(devices) == "table") then
            for _, dev_name in pairs(devices) do
                add_dev(step, action_name, dev_name, group_number,
                    sub_action_name)
            end
        else
            local parameter_value = devices
            local parameter_name = sub_action_name
            step:AddParam(action_name, parameter_value, parameter_name,
                group_number)
        end
    end
end

function proc_action_custom(step, group, action_name, group_number)
    local action_number = 0;
    local parameter_number = 0;
    for sub_action_name, data in pairs (group) do
        if (type(data) == "table") then
            for _, dev_name in pairs (data) do
                if type(tonumber(sub_action_name)) == 'number' then
                    sub_action_name = "A_"..tostring(action_number)
                end
                add_dev(step, action_name, dev_name, 
                    group_number, sub_action_name)
            end
            action_number = action_number + 1
        else
            if type(tonumber(sub_action_name)) == 'number' then
                sub_action_name = "P_"..tostring(parameter_number)
            end
            if data ~= -1 then
                step:AddParam(action_name, data, sub_action_name,
                    group_number)
            end
            parameter_number = parameter_number + 1
        end
    end
end

function proc_action_custom_groups(step, actions, action_name)
    for current_action_name, action in pairs(actions) do
        if action_name == current_action_name then
            local group_number = 0   
            local parameter_number = 0;
            for group_name, group in pairs(action) do
                if type(group) == "table" then
                    if has_table(group) then
                        proc_action_custom(step, group, action_name, group_number)
                    else
                        for _, dev_name in pairs (group) do
                            if type(tonumber(group_name)) == 'number' then
                                group_name = "A_0"
                            end
                            add_dev(step, action_name, dev_name, 
                                group_number, group_name)
                        end
                    end
                    group_number = group_number + 1
                else
                    if type(tonumber(group_name)) == 'number' then
                        group_name = "P_"..tostring(parameter_number)
                    end
                    step:AddParam(action_name, group, group_name, -1)
                    parameter_number = parameter_number + 1
                end
            end
        end
    end
end

function  has_table(table)
	for _, value in pairs(table) do
        if type(value) == "table" then
            return true
        end
    end
    return false
end

function add_dev(step, action_name, dev_name, group_number, sub_action_name)
	step:AddDev(action_name, dev_name, group_number, sub_action_name)
end