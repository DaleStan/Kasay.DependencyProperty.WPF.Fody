﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Linq;

partial class ModuleWeaver
{
    FieldDefinition dependencyPropertyField;

    void DependencyPropertyFactory(TypeDefinition typeDefinition, PropertyDefinition propertyDefinition)
    {
        AddDependencyPropertyField(typeDefinition, propertyDefinition);
        EqualDependencyPropertyField(typeDefinition, propertyDefinition);
        ModifyGetMethod(propertyDefinition);
        ModifySetMethod(propertyDefinition);
    }

    void AddDependencyPropertyField(TypeDefinition typeDefinition, PropertyDefinition propertyDefinition)
    {
        var nameField = $"{propertyDefinition.Name}Property";

        dependencyPropertyField = new FieldDefinition(
            nameField,
            FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly,
            baseAssembly.GetTypeReference("System.Windows.DependencyProperty"));

        typeDefinition.Fields.Add(dependencyPropertyField);
    }

    void EqualDependencyPropertyField(TypeDefinition typeDefinition, PropertyDefinition propertyDefinition)
    {
        var method = typeDefinition.GetStaticConstructor();

        var nameProperty = propertyDefinition.Name;
        var typeProperty = propertyDefinition.PropertyType;
        var callTypeOf = ModuleDefinition.GetMethodReference("System.Type", "GetTypeFromHandle");
        var callRegister = baseAssembly.GetMethodReference("System.Windows.DependencyProperty", "Register", 3);

        method.Body.Instructions.RemoveAt(method.Body.Instructions.Count - 1);

        var processor = method.Body.GetILProcessor();
        processor.Emit(OpCodes.Ldstr, nameProperty);
        processor.Emit(OpCodes.Ldtoken, typeProperty);
        processor.Emit(OpCodes.Call, callTypeOf);
        processor.Emit(OpCodes.Ldtoken, typeDefinition);
        processor.Emit(OpCodes.Call, callTypeOf);
        processor.Emit(OpCodes.Call, callRegister);
        processor.Emit(OpCodes.Stsfld, dependencyPropertyField);
        processor.Emit(OpCodes.Ret);
    }

    void ModifyGetMethod(PropertyDefinition propertyDefinition)
    {
        var callGetValue = baseAssembly.GetMethodReference("System.Windows.DependencyObject", "GetValue");
        var typeProperty = propertyDefinition.PropertyType;

        propertyDefinition.GetMethod.Body.Instructions.Clear();

        var processor = propertyDefinition.GetMethod.Body.GetILProcessor();
        processor.Emit(OpCodes.Nop);
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Ldsfld, dependencyPropertyField);
        processor.Emit(OpCodes.Call, callGetValue);
        processor.Emit(OpCodes.Unbox_Any, typeProperty);
        processor.Emit(OpCodes.Ret);
    }

    void ModifySetMethod(PropertyDefinition propertyDefinition)
    {
        var typeProperty = propertyDefinition.PropertyType;
        var callSetValue = baseAssembly.GetMethodReference("System.Windows.DependencyObject", "SetValue");

        propertyDefinition.SetMethod.Body.Instructions.Clear();

        var processor = propertyDefinition.SetMethod.Body.GetILProcessor();
        processor.Emit(OpCodes.Nop);
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Ldsfld, dependencyPropertyField);
        processor.Emit(OpCodes.Ldarg_1);
        processor.Emit(OpCodes.Box, typeProperty);
        processor.Emit(OpCodes.Call, callSetValue);
        processor.Emit(OpCodes.Nop);
        processor.Emit(OpCodes.Ret);
    }
}
