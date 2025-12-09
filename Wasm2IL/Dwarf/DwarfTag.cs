namespace Wasm2IL.Dwarf;

/// <summary>
/// DWARF tag encodings as defined in the DWARF Debugging Information Format Version 4
/// Source: Figure 18 - Tag encodings (page 154)
/// </summary>
public enum DwarfTag : uint
{
    // Array and string types
    DW_TAG_array_type = 0x01,
    DW_TAG_string_type = 0x12,
    DW_TAG_set_type = 0x20,
    DW_TAG_packed_type = 0x2d,

    // Class and structure types
    DW_TAG_class_type = 0x02,
    DW_TAG_structure_type = 0x13,
    DW_TAG_union_type = 0x17,
    DW_TAG_interface_type = 0x38,

    // Type-related tags
    DW_TAG_enumeration_type = 0x04,
    DW_TAG_pointer_type = 0x0f,
    DW_TAG_reference_type = 0x10,
    DW_TAG_ptr_to_member_type = 0x1f,
    DW_TAG_rvalue_reference_type = 0x42,
    DW_TAG_subroutine_type = 0x15,
    DW_TAG_base_type = 0x24,
    DW_TAG_typedef = 0x16,
    DW_TAG_const_type = 0x26,
    DW_TAG_volatile_type = 0x35,
    DW_TAG_restrict_type = 0x37,
    DW_TAG_unspecified_type = 0x3b,
    DW_TAG_shared_type = 0x40,

    // Subrange and dimension types
    DW_TAG_subrange_type = 0x21,

    // Entry points and compilation
    DW_TAG_entry_point = 0x03,
    DW_TAG_compile_unit = 0x11,
    DW_TAG_partial_unit = 0x3c,
    DW_TAG_type_unit = 0x41,

    // Subprograms and procedures
    DW_TAG_subprogram = 0x2e,
    DW_TAG_inlined_subroutine = 0x1d,
    DW_TAG_dwarf_procedure = 0x36,

    // Parameters and variables
    DW_TAG_formal_parameter = 0x05,
    DW_TAG_variable = 0x34,
    DW_TAG_constant = 0x27,

    // Members and related
    DW_TAG_member = 0x0d,
    DW_TAG_friend = 0x2a,
    DW_TAG_inheritance = 0x1c,

    // Labels and blocks
    DW_TAG_label = 0x0a,
    DW_TAG_lexical_block = 0x0b,

    // Exception handling
    DW_TAG_catch_block = 0x25,
    DW_TAG_try_block = 0x32,
    DW_TAG_variant = 0x19,
    DW_TAG_variant_part = 0x33,
    DW_TAG_thrown_type = 0x31,

    // Modules and namespaces
    DW_TAG_module = 0x1e,
    DW_TAG_namespace = 0x39,
    DW_TAG_imported_module = 0x3a,
    DW_TAG_imported_unit = 0x3d,

    // Import declarations
    DW_TAG_imported_declaration = 0x08,
    DW_TAG_access_declaration = 0x23,

    // With statements
    DW_TAG_with_stmt = 0x22,

    // Common blocks
    DW_TAG_common_block = 0x1a,
    DW_TAG_common_inclusion = 0x1b,

    // Enumeration
    DW_TAG_enumerator = 0x28,

    // File types
    DW_TAG_file_type = 0x29,

    // Name lists
    DW_TAG_namelist = 0x2b,
    DW_TAG_namelist_item = 0x2c,

    // Unspecified parameters
    DW_TAG_unspecified_parameters = 0x18,

    // Templates
    DW_TAG_template_type_parameter = 0x2f,
    DW_TAG_template_value_parameter = 0x30,
    DW_TAG_template_alias = 0x43,

    // Conditions
    DW_TAG_condition = 0x3f,

    // Vendor-specific extensions
    DW_TAG_lo_user = 0x4080,
    DW_TAG_hi_user = 0xffff
}