<template>
	<div class="chatModelOptions-container">
		<el-dialog v-model="isShowDialog" :width="800" draggable="">
			<template #header>
				<div style="color: #fff">
					<!--<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>-->
					<span>{{ props.title }}</span>
				</div>
			</template>
			<el-form :model="ruleForm" ref="ruleFormRef" label-width="auto" :rules="rules">
				<el-row :gutter="35">
					<el-form-item v-show="false">
						<el-input v-model="ruleForm.id" />
					</el-form-item>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="对应的模型" prop="modelId">
							<el-select clearable filterable v-model="ruleForm.modelId" placeholder="请选择对应的模型">
								<el-option v-for="(item,index) in chatModelModelIdDropdownList" :key="index" :value="item.value" :label="item.label" />
								
							</el-select>
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="配置名称" prop="name">
							<el-input v-model="ruleForm.name" placeholder="请输入配置名称" maxlength="20" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="配置类型" prop="optionType">
							<el-select clearable v-model="ruleForm.optionType" placeholder="请选择配置类型">
								<el-option v-for="(item,index) in  getEnumOptionTypeData" :key="index" :value="item.value" :label="`[${item.value}] ${item.describe}`"></el-option>
								
							</el-select>
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="最大值" prop="maxNum">
							<el-input v-model="ruleForm.maxNum" placeholder="请输入最大值" maxlength="0" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="最小值" prop="minNum">
							<el-input v-model="ruleForm.minNum" placeholder="请输入最小值" maxlength="0" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="选择的值，逗号分割" prop="selectedValues">
							<el-input v-model="ruleForm.selectedValues" placeholder="请输入选择的值，逗号分割" maxlength="2000" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="模型描述" prop="desc">
							<el-input v-model="ruleForm.desc" placeholder="请输入模型描述" maxlength="4000" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="默认值" prop="defaultVal">
							<el-input v-model="ruleForm.defaultVal" placeholder="请输入默认值" maxlength="4000" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="扩展" prop="extends">
							<el-input v-model="ruleForm.extends" placeholder="请输入扩展" maxlength="4000" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="配置字段" prop="field">
							<el-input v-model="ruleForm.field" placeholder="请输入配置字段" maxlength="20" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">取 消</el-button>
					<el-button type="primary" @click="submit">确 定</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>
<style scoped>
:deep(.el-select),
:deep(.el-input-number) {
	width: 100%;
}
</style>
<script lang="ts" setup>
	import { ref,onMounted } from "vue";
	import { getDictDataItem as di, getDictDataList as dl } from '/@/utils/dict-utils';
	import { ElMessage } from "element-plus";
	import type { FormRules } from "element-plus";
	import { addChatModelOptions, updateChatModelOptions, detailChatModelOptions } from "/@/api/main/chatModelOptions";
	import { getChatModelModelIdDropdown } from '/@/api/main/chatModelOptions';
	import { getAPI } from '/@/utils/axios-utils';
	import { SysEnumApi } from '/@/api-services/api';

	const getEnumOptionTypeData = ref<any>([]);
	//父级传递来的参数
	var props = defineProps({
		title: {
		type: String,
		default: "",
	},
	});
	//父级传递来的函数，用于回调
	const emit = defineEmits(["reloadTable"]);
	const ruleFormRef = ref();
	const isShowDialog = ref(false);
	const ruleForm = ref<any>({});
	//自行添加其他规则
	const rules = ref<FormRules>({
		name: [{required: true, message: '请输入配置名称！', trigger: 'blur',},],
		maxNum: [{required: true, message: '请输入最大值！', trigger: 'blur',},],
		minNum: [{required: true, message: '请输入最小值！', trigger: 'blur',},],
		selectedValues: [{required: true, message: '请输入选择的值，逗号分割！', trigger: 'blur',},],
		field: [{required: true, message: '请输入配置字段！', trigger: 'blur',},],
	});

	// 打开弹窗
	const openDialog = async (row: any) => {
		// ruleForm.value = JSON.parse(JSON.stringify(row));
		// 改用detail获取最新数据来编辑
		let rowData = JSON.parse(JSON.stringify(row));
		if (rowData.id)
			ruleForm.value = (await detailChatModelOptions(rowData.id)).data.result;
		else
			ruleForm.value = rowData;
		isShowDialog.value = true;
	};

	// 关闭弹窗
	const closeDialog = () => {
		emit("reloadTable");
		isShowDialog.value = false;
	};

	// 取消
	const cancel = () => {
		isShowDialog.value = false;
	};

	// 提交
	const submit = async () => {
		ruleFormRef.value.validate(async (isValid: boolean, fields?: any) => {
			if (isValid) {
				let values = ruleForm.value;
				if (ruleForm.value.id == undefined || ruleForm.value.id == null || ruleForm.value.id == "" || ruleForm.value.id == 0) {
					await addChatModelOptions(values);
				} else {
					await updateChatModelOptions(values);
				}
				closeDialog();
			} else {
				ElMessage({
					message: `表单有${Object.keys(fields).length}处验证失败，请修改后再提交`,
					type: "error",
				});
			}
		});
	};

	const chatModelModelIdDropdownList = ref<any>([]); 
	const getChatModelModelIdDropdownList = async () => {
		let list = await getChatModelModelIdDropdown();
		chatModelModelIdDropdownList.value = list.data.result ?? [];
	};
	getChatModelModelIdDropdownList();
	






	// 页面加载时
	onMounted(async () => {
			getEnumOptionTypeData.value = (await getAPI(SysEnumApi).apiSysEnumEnumDataListGet('AIModelOptionEnum')).data.result ?? [];
	});

	//将属性或者函数暴露给父组件
	defineExpose({ openDialog });
</script>




