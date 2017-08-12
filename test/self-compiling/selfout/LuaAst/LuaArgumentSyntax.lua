-- Generated by CSharp.lua Compiler 1.1.0
--[[
Copyright 2017 YANG Huan (sy.yanghuan@gmail.com).

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
]]
local System = System
local CSharpLua
local CSharpLuaLuaAst
System.usingDeclare(function (global) 
  CSharpLua = global.CSharpLua
  CSharpLuaLuaAst = CSharpLua.LuaAst
end)
System.namespace("CSharpLua.LuaAst", function (namespace) 
  namespace.class("LuaArgumentSyntax", function (namespace) 
    local Render, __ctor__
    __ctor__ = function (this, expression) 
      this.__base__.__ctor__(this)
      if expression == nil then
        System.throw(CSharpLua.ArgumentNullException("expression" --[[nameof(expression)]]))
      end
      this.Expression = expression
    end
    Render = function (this, renderer) 
      renderer:Render8(this)
    end
    return {
      __inherits__ = function (global) 
        return {
          global.CSharpLua.LuaAst.LuaSyntaxNode
        }
      end, 
      Render = Render, 
      __ctor__ = __ctor__
    }
  end)

  namespace.class("LuaArgumentListSyntax", function (namespace) 
    local getOpenParenToken, getCloseParenToken, Render, AddArgument, AddArgument1, AddArguments, __init__, __ctor__
    __init__ = function (this) 
      this.Arguments = CSharpLuaLuaAst.LuaSyntaxList_1(CSharpLuaLuaAst.LuaArgumentSyntax)()
    end
    __ctor__ = function (this) 
      __init__(this)
      this.__base__.__ctor__(this)
    end
    getOpenParenToken = function (this) 
      return "(" --[[Tokens.OpenParentheses]]
    end
    getCloseParenToken = function (this) 
      return ")" --[[Tokens.CloseParentheses]]
    end
    Render = function (this, renderer) 
      renderer:Render7(this)
    end
    AddArgument = function (this, argument) 
      this.Arguments:Add(argument)
    end
    AddArgument1 = function (this, argument) 
      AddArgument(this, CSharpLuaLuaAst.LuaArgumentSyntax(argument))
    end
    AddArguments = function (this, arguments) 
      for _, argument in System.each(arguments) do
        AddArgument1(this, argument)
      end
    end
    return {
      __inherits__ = function (global) 
        return {
          global.CSharpLua.LuaAst.LuaSyntaxNode
        }
      end, 
      getOpenParenToken = getOpenParenToken, 
      getCloseParenToken = getCloseParenToken, 
      Render = Render, 
      AddArgument = AddArgument, 
      AddArgument1 = AddArgument1, 
      AddArguments = AddArguments, 
      __ctor__ = __ctor__
    }
  end)
end)
